namespace DBot.Bot.Configuration;

/// <summary>
/// Configuration for multiple Discord bots
/// </summary>
public class DiscordBotsConfiguration
{
    /// <summary>
    /// Collection of Discord bot configurations
    /// </summary>
    public List<DiscordBotInstance> Bots { get; set; } = [];
}

/// <summary>
/// Configuration for a specific Discord bot instance
/// </summary>
public class DiscordBotInstance
{
    /// <summary>
    /// Unique identifier for this bot instance
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Discord bot token
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Test guild IDs for faster command registration during development
    /// </summary>
    public ulong[] TestGuilds { get; set; } = [];
}