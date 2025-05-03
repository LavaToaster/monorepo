using System.Collections.Concurrent;
using DBot.Bot.Configuration;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace DBot.Bot.Services;

/// <summary>
/// Service that manages multiple Discord bot connections
/// </summary>
public class DiscordBotManager(
    ILogger<DiscordBotManager> logger,
    IOptions<DiscordBotsConfiguration> botsConfig,
    IServiceProvider serviceProvider)
{
    private readonly DiscordBotsConfiguration _botsConfig = botsConfig.Value;
    private readonly ConcurrentDictionary<string, BotInstance> _botInstances = new();
    
    public async Task InitialiseBotsAsync()
    {
        foreach (var botConfig in _botsConfig.Bots)
        {
            await CreateBotInstanceAsync(botConfig);
        }
        
        logger.LogInformation("Initialised {count} Discord bot instances", _botInstances.Count);
    }
    
    private Task<BotInstance> CreateBotInstanceAsync(DiscordBotInstance config)
    {
        var socketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.Guilds | GatewayIntents.GuildMessages,
            AlwaysDownloadUsers = true
        };

        var client = new DiscordSocketClient(socketConfig);
        
        var interactionConfig = new InteractionServiceConfig
        {
            DefaultRunMode = RunMode.Async,
            LogLevel = LogSeverity.Info
        };
        
        var interactions = new InteractionService(client, interactionConfig);
        
        var botInstance = new BotInstance(config.Id, client, interactions, config);
        
        if (_botInstances.TryAdd(config.Id, botInstance))
        {
            logger.LogInformation("Created bot instance '{BotId}'", config.Id);
            return Task.FromResult(botInstance);
        }
        
        logger.LogWarning("Bot instance with ID '{BotId}' already exists", config.Id);
        return Task.FromResult(_botInstances[config.Id]);
    }
    
    public IEnumerable<BotInstance> GetAllBots() => _botInstances.Values;
    
    public void AddGuildMemberUpdatedHandler<T>(Func<T, SocketGuildUser, Task> handler) where T : class
    {
        foreach (var bot in _botInstances.Values)
        {
            bot.Client.GuildMemberUpdated += async (before, after) =>
            {
                if (!before.HasValue) return;
                
                using var scope = serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<T>();
                await handler(service, after);
            };
        }
    }
    
    public BotInstance GetBotForGuild(ulong guildId)
    {
        // Find the bot that has this guild in its cache
        foreach (var bot in _botInstances.Values)
        {
            if (bot.Client.Guilds.Any(g => g.Id == guildId))
            {
                return bot;
            }
        }
        
        throw new InvalidOperationException($"No bot instance found for guild ID '{guildId}'");
    }
}

/// <summary>
/// Represents a Discord bot instance
/// </summary>
public class BotInstance(
    string id,
    DiscordSocketClient client,
    InteractionService interactions,
    DiscordBotInstance config)
{
    public string Id { get; } = id;
    public DiscordSocketClient Client { get; } = client;
    public InteractionService Interactions { get; } = interactions;
    public DiscordBotInstance Config { get; } = config;
}