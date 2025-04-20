namespace DBot.Bot.Configuration;

public class DiscordConfiguration
{
    public required string Token { get; set; }

    // public required string GameStatus { get; set; }
    public ulong[]? TestGuilds { get; set; }
}