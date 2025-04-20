using Discord;

namespace DBot.Bot.Embeds;

public static class StatusEmbedGenerator
{
    public static Embed Success(string title, string description)
    {
        return new EmbedBuilder()
            .WithTitle($"✅ {title}")
            .WithDescription(description)
            .WithColor(Color.Green)
            .Build();
    }
    
    public static Embed Error(string description)
    {
        return new EmbedBuilder()
            .WithTitle("❌ Error")
            .WithDescription(description)
            .WithColor(Color.Red)
            .Build();
    }
}