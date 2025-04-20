using DBot.Bot.Embeds;
using DBot.Core.Data.Entities;
using DBot.Integrations.Assetto;
using DBot.Integrations.Assetto.Models;
using Discord;
using Discord.WebSocket;

namespace DBot.Bot.Services;

/// <summary>
///     Handles the generation and updating of Discord server status messages
/// </summary>
public class AssettoStatusMessageGenerator(
    DiscordSocketClient discordClient,
    AssettoServerClientFactory clientFactory,
    AssettoServerEmbedFactory embedFactory,
    ILogger<AssettoStatusMessageGenerator> logger)
{
    public async Task CreateServerStatusMessageAsync(AssettoServerMonitorEntity monitor,
        AssettoServerEntity server)
    {
        try
        {
            var client = clientFactory.CreateClient(server.ApiUrl);

            var details = await client.GetServerDetailsAsync();

            await UpdateServerStatusMessageAsync(monitor, details);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create status message for server {ServerId}", server.Id);
            throw;
        }
    }

    public async Task UpdateServerStatusMessageAsync(AssettoServerMonitorEntity monitor, DetailResponse? details)
    {
        try
        {
            var guild = discordClient.GetGuild(monitor.GuildId);

            if (guild == null)
            {
                logger.LogWarning("Could not find guild {GuildId} for status message {MessageId}",
                    monitor.GuildId, monitor.MessageId);
                return;
            }

            var channel = guild.GetTextChannel(monitor.ChannelId);

            if (channel == null)
            {
                logger.LogWarning("Could not find channel {ChannelId} in guild {GuildId}",
                    monitor.ChannelId, monitor.GuildId);
                return;
            }

            var message = await channel.GetMessageAsync(monitor.MessageId) as IUserMessage;

            if (message == null)
            {
                logger.LogWarning("Could not find message {MessageId} in channel {ChannelId}",
                    monitor.MessageId, monitor.ChannelId);
                return;
            }

            var embed = embedFactory.CreateServerStatusEmbed(monitor, details);

            await message.ModifyAsync(msg =>
            {
                msg.Embed = embed;

                if ((message.Flags & MessageFlags.SuppressEmbeds) == MessageFlags.SuppressEmbeds)
                {
                    msg.Flags = message.Flags & ~MessageFlags.SuppressEmbeds;
                }
                
                // if (details is not null)
                // {
                //     var button = new ComponentBuilder()
                //         .WithButton("Drive now",
                //             emote: Emote.Parse("<:aegis_participant:1301470357865365504>"),
                //             style: ButtonStyle.Link,
                //             url: $"https://acstuff.ru/s/q:race/online/join?ip={details.Ip}&httpPort={details.WrappedPort}");
                //     
                //     msg.Components = button.Build();
                // }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update status message {MessageId} in channel {ChannelId}",
                monitor.MessageId, monitor.ChannelId);
            throw;
        }
    }
}