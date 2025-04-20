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
    IOptions<DiscordConfiguration> discordOptions,
    DiscordSocketClient client,
    InteractionService interactions,
    IServiceProvider services
)
    : BackgroundService
{
    private readonly DiscordConfiguration _discordConfig = discordOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Set up logging
        client.Log += OnClientLog;

        // Set up event handlers
        client.Ready += OnClientReady;
        client.InteractionCreated += OnInteractionCreated;
        client.GuildMemberUpdated += OnGuildMemberUpdated;

        logger.LogInformation("Discord configuration loaded: Token={Token}, TestGuilds={TestGuilds}", _discordConfig.Token?.Substring(0,8) + "...", _discordConfig.TestGuilds);

        // Connect to Discord
        var token = _discordConfig.Token ??
                    throw new InvalidOperationException("Discord Token not found in configuration");

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        logger.LogInformation("Discord bot started at: {time}", DateTimeOffset.Now);

        // Keep the service running until cancellation is requested
        await Task.Delay(Timeout.Infinite, stoppingToken);

        // Clean up
        await client.StopAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // TODO: This is nice for testing, but should be removed in production
        //  as users might have multiple bots running on the same token
        await client.SetStatusAsync(UserStatus.Invisible);
        await client.StopAsync();

        await base.StopAsync(cancellationToken);
    }

    private async Task OnClientReady()
    {
        try
        {
            logger.LogInformation("Discord client ready, registering commands...");

            // Add all command modules from the assembly
            await interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), services);

            // Register commands globally
            if (_discordConfig.TestGuilds?.Length > 0)
            {
                // Register commands to debug guilds for faster testing
                var testGuildIds = _discordConfig.TestGuilds;

                foreach (var guildId in testGuildIds)
                {
                    await interactions.RegisterCommandsToGuildAsync(guildId);
                    logger.LogInformation("Commands registered to test guild {GuildId}", guildId);
                }
            }
            else
            {
                await interactions.RegisterCommandsGloballyAsync();
                logger.LogInformation("Commands registered globally");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during client ready handling");
        }
    }

    private async Task OnInteractionCreated(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(client, interaction);
            await interactions.ExecuteCommandAsync(context, services);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling interaction {InteractionId}", interaction.Id);

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

    private Task OnClientLog(LogMessage msg)
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

        logger.Log(level, msg.Exception, "[Discord Client] {Source}: {Message}", msg.Source, msg.Message);
        return Task.CompletedTask;
    }
}