using DBot.Bot.Services;
using DBot.Bot.Util.Time;
using DBot.Core.Data.Entities;
using DBot.Integrations.Assetto.Models;
using DBot.Integrations.Assetto.Util;
using Discord;

namespace DBot.Bot.Embeds;

public class AssettoServerEmbedFactory(IServiceProvider serviceProvider)
{
    private readonly DiscordBotManager _botManager = serviceProvider.GetRequiredService<DiscordBotManager>();

    public Embed CreateServerStatusEmbed(AssettoServerMonitorEntity monitor, DetailResponse? details)
    {
        // Get the appropriate bot for this monitor based on the guild ID
        var botUser = GetBotUser(monitor.GuildId);
        
        var embedBuilder = new EmbedBuilder()
            .WithColor(0xFFE743)
            .WithAuthor(botUser.GlobalName)
            .WithDescription(monitor.Description)
            .WithThumbnailUrl(monitor.ThumbnailUrl);

        if (details is not null)
        {
            var (hours, minutes, formatted) = TimeUtil.ConvertTimeOfDayToHoursAndMinutes(details.TimeOfDay);

            embedBuilder.AddField("Status: ", ":green_circle: Online", true)
                .AddField("\u200b", "\u200b", true)
                .AddField("Drivers: ", $":busts_in_silhouette: {details.Clients}/{details.MaxClients}", true)
                .AddField("Location: ", $"🇫🇷 {details.Country?.FirstOrDefault() ?? "Unknown"}", true)
                .AddField("\u200b", "\u200b", true)
                .AddField("Address: ", $":link: `{details.Ip}:{details.WrappedPort}`", true)
                .AddField("Time: ", $"{ClockFaceUtil.TimeToEmoji(hours, minutes)} `{formatted}`",
                    true)
                .AddField("\u200b", "\u200b", true)
                .AddField("Weather: ", $":white_sun_cloud: `{details.CurrentWeatherId}`", true);
        }
        else
        {
            embedBuilder.WithColor(0xFFB3B3);
            embedBuilder.AddField("Status: ", ":red_circle: Offline", true);
        }

        // Add a custom thumbnail if available, otherwise use the server's loading image
        if (!string.IsNullOrEmpty(monitor.ThumbnailUrl))
            embedBuilder.WithThumbnailUrl(monitor.ThumbnailUrl);
        else if (!string.IsNullOrEmpty(details?.LoadingImageUrl))
            embedBuilder.WithThumbnailUrl(details.LoadingImageUrl);

        if (details?.LoadingImageUrl?.Length > 0) embedBuilder.WithImageUrl(details.LoadingImageUrl);

        embedBuilder.WithFooter("Last updated")
            .WithCurrentTimestamp();

        return embedBuilder.Build();
    }
    
    private IUser GetBotUser(ulong guildId)
    {
        var botInstance = _botManager.GetBotForGuild(guildId);
        var guild = botInstance.Client.GetGuild(guildId);

        if (guild == null)
        {
            throw new InvalidOperationException($"Guild {guildId} not found");
        }

        return guild.CurrentUser;
    }
}