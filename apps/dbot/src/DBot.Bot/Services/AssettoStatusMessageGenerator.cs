using DBot.Bot.Embeds;
using DBot.Core.Data.Context;
using DBot.Core.Data.Entities;
using DBot.Integrations.Assetto;
using DBot.Integrations.Assetto.Models;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace DBot.Bot.Services;

/// <summary>
///     Handles the generation and updating of Discord server status messages
/// </summary>
public class AssettoStatusMessageGenerator(
    ILogger<AssettoStatusMessageGenerator> logger,
    AssettoServerClientFactory clientFactory,
    AssettoServerEmbedFactory embedFactory,
    DiscordBotManager botManager)
{
    public async Task<bool> CreateServerStatusMessageAsync(
        AssettoServerMonitorEntity monitor, 
        AssettoServerEntity server, 
        DiscordSocketClient? client = null)
    {
        try
        {
            if (client == null)
            {
                var botInstance = botManager.GetBotForGuild(monitor.GuildId);
                client = botInstance.Client;
            }
            
            // Get the server details
            var assettoClient = clientFactory.CreateClient(server.ApiUrl);
            DetailResponse? details = null;

            try
            {
                details = await assettoClient.GetServerDetailsAsync();
            }
            catch
            {
                // Server might be offline, that's ok - we'll show the offline status
            }

            // Create the embed
            var embed = embedFactory.CreateServerStatusEmbed(monitor, details);

            // Get the channel and message
            var channel = await client.GetChannelAsync(monitor.ChannelId) as IMessageChannel;
            if (channel == null)
            {
                logger.LogError("Channel {ChannelId} not found", monitor.ChannelId);
                return false;
            }

            var message = await channel.GetMessageAsync(monitor.MessageId) as IUserMessage;
            if (message == null)
            {
                logger.LogError("Message {MessageId} not found in channel {ChannelId}", monitor.MessageId, monitor.ChannelId);
                return false;
            }

            // Update the message
            await message.ModifyAsync(props => props.Embed = embed);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating server status message for {ServerUrl}", server.ApiUrl);
            return false;
        }
    }

    public async Task<bool> UpdateServerStatusMessageAsync(
        AssettoServerMonitorEntity monitor, 
        DetailResponse? details, 
        DiscordSocketClient? client = null)
    {
        try
        {
            if (client == null)
            {
                var botInstance = botManager.GetBotForGuild(monitor.GuildId);
                client = botInstance.Client;
            }
            
            // Create the embed
            var embed = embedFactory.CreateServerStatusEmbed(monitor, details);

            // Get the channel and message
            var channel = await client.GetChannelAsync(monitor.ChannelId) as IMessageChannel;
            if (channel == null)
            {
                logger.LogError("Channel {ChannelId} not found", monitor.ChannelId);
                return false;
            }

            var message = await channel.GetMessageAsync(monitor.MessageId) as IUserMessage;
            if (message == null)
            {
                logger.LogError("Message {MessageId} not found in channel {ChannelId}", monitor.MessageId, monitor.ChannelId);
                return false;
            }

            // Update the message
            await message.ModifyAsync(props => props.Embed = embed);
            return true;
        }
        catch (Exception ex)
        {
            // If this is a Discord.Net error about unknown channel or message, just log as debug
            if (ex is Discord.Net.HttpException || ex.Message.Contains("not found"))
            {
                logger.LogDebug("Message or channel not found for monitor {MonitorId}: {Error}", monitor.Id, ex.Message);
            }
            else
            {
                logger.LogError(ex, "Error updating server status message for monitor {MonitorId}", monitor.Id);
            }
            
            return false;
        }
    }
}