using System.Reflection;
using DBot.Bot.Configuration;
using DBot.Bot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
    
namespace DBot.Bot.Hosting;

/// <summary>
///     Host service responsible for connecting to Discord and handling interactions
/// </summary>
public class DiscordBotService(
    ILogger<DiscordBotService> logger,
    IOptions<DiscordBotsConfiguration> discordBotsOptions,
    DiscordBotManager botManager,
    IServiceProvider services
)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Initialising {count} bots", discordBotsOptions.Value.Bots.Count);
            await InitialiseBotsAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initialising Discord bot service");
        }
    }

    private async Task InitialiseBotsAsync(CancellationToken stoppingToken)
    {
        // Initialize all bot instances
        await botManager.InitialiseBotsAsync();
        
        foreach (var botInstance in botManager.GetAllBots())
        {
            // Set up logging
            botInstance.Client.Log += msg => OnClientLog(msg, botInstance.Id);

            // Set up event handlers
            botInstance.Client.Ready += () => OnClientReady(botInstance);
            botInstance.Client.InteractionCreated += interaction => OnInteractionCreated(interaction, botInstance);
            botInstance.Client.GuildMemberUpdated += OnGuildMemberUpdated;

            
            // Connect to Discord
            logger.LogInformation("Connecting bot '{BotId}' to Discord", botInstance.Id);
            
            var token = botInstance.Config.Token;
            if (string.IsNullOrEmpty(token))
            {
                logger.LogError("Discord token not found for bot '{BotId}'", botInstance.Id);
                continue;
            }

            await botInstance.Client.LoginAsync(TokenType.Bot, token);
            await botInstance.Client.StartAsync();
        }
        
        // Register the guild member updated handler for role syncing
        botManager.AddGuildMemberUpdatedHandler<RoleMirrorService>(async (service, after) => 
        {
            // Make sure we have the before user cached
            var beforeUser = await ((IGuild)after.Guild).GetUserAsync(after.Id);
            if (beforeUser == null)
                return;
                
            // Check if roles were changed by comparing the role collections
            if (!beforeUser.RoleIds.SequenceEqual(after.Roles.Select(r => r.Id)))
            {
                // Determine which roles were added
                var addedRoles = after.Roles
                    .Where(r => !beforeUser.RoleIds.Contains(r.Id))
                    .Select(r => r.Id)
                    .ToArray();
                
                // Determine which roles were removed
                var removedRoles = beforeUser.RoleIds
                    .Where(id => after.Roles.All(r => r.Id != id))
                    .ToArray();

                await service.UpdateUserRole(after.Guild.Id, after, addedRoles, removedRoles);
            }
        });

        // Keep the service running until cancellation is requested
        await Task.Delay(Timeout.Infinite, stoppingToken);
        
        // Clean up
        foreach (var botInstance in botManager.GetAllBots())
        {
            await botInstance.Client.SetStatusAsync(UserStatus.Invisible);
            await botInstance.Client.StopAsync();
        }
    }

    private async Task OnClientReady(BotInstance botInstance)
    {
        try
        {
            logger.LogInformation("Discord client for bot '{BotId}' ready, registering commands...", botInstance.Id);

            // Add all command modules from the assembly
            await botInstance.Interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), services);

            // Register commands globally or to test guilds
            if (botInstance.Config.TestGuilds?.Length > 0)
            {
                // Register commands to debug guilds for faster testing
                var testGuildIds = botInstance.Config.TestGuilds;

                foreach (var guildId in testGuildIds)
                {
                    await botInstance.Interactions.RegisterCommandsToGuildAsync(guildId);
                    logger.LogInformation("Commands registered to test guild {GuildId} for bot '{BotId}'", guildId, botInstance.Id);
                }
            }
            else
            {
                await botInstance.Interactions.RegisterCommandsGloballyAsync();
                logger.LogInformation("Commands registered globally for bot '{BotId}'", botInstance.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during client ready handling for bot '{BotId}'", botInstance.Id);
        }
    }

    private async Task OnInteractionCreated(SocketInteraction interaction, BotInstance botInstance)
    {
        try
        {
            var context = new SocketInteractionContext(botInstance.Client, interaction);
            await botInstance.Interactions.ExecuteCommandAsync(context, services);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling interaction {InteractionId} for bot '{BotId}'", interaction.Id, botInstance.Id);

            // If this is not an ACK'd interaction, respond with an error
            if (interaction.Type is InteractionType.ApplicationCommand or
                InteractionType.MessageComponent or
                InteractionType.ModalSubmit)
            {
                if (!interaction.HasResponded)
                    await interaction.RespondAsync("An error occurred while processing the command.", ephemeral: true);
                else
                    await interaction.FollowupAsync("An error occurred while processing the command.", ephemeral: true);
            }
        }
    }

    private async Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        using var scope = services.CreateScope();
        var mirrorService = scope.ServiceProvider.GetRequiredService<RoleMirrorService>();
        
        // Make sure we have the before user cached
        if (!before.HasValue)
            return;

        var beforeUser = before.Value;
        
        // Check if roles were changed by comparing the role collections
        if (!beforeUser.Roles.SequenceEqual(after.Roles))
        {
            // Determine which roles were added
            var addedRoles = after.Roles
                .Where(r => beforeUser.Roles.All(r2 => r2.Id != r.Id))
                .Select(r => r.Id)
                .ToArray();
            
            // Determine which roles were removed
            var removedRoles = beforeUser.Roles
                .Where(r => after.Roles.All(r2 => r2.Id != r.Id))
                .Select(r => r.Id)
                .ToArray();

            await mirrorService.UpdateUserRole(after.Guild.Id, after, addedRoles, removedRoles);
        }
    }

    private Task OnClientLog(LogMessage msg, string botId)
    {
        var level = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        logger.Log(level, msg.Exception, "[Discord Client - {BotId}] {Source}: {Message}", botId, msg.Source, msg.Message);
        return Task.CompletedTask;
    }
}