using DBot.Bot.Services;
using DBot.Core.Data.Context;
using DBot.Core.Data.Entities;
using DBot.Integrations.Assetto;
using DBot.Integrations.Assetto.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace DBot.Bot.Hosting;

/// <summary>
///     Background service that periodically monitors Assetto servers and updates Discord status messages
/// </summary>
public class AssettoServerMonitorService(
    IServiceScopeFactory scopeFactory,
    DiscordBotManager botManager,
    AssettoServerClientFactory clientFactory,
    AssettoStatusMessageGenerator messageGenerator,
    ILogger<AssettoServerMonitorService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Assetto server monitor service is starting");

        // Wait until Discord clients are ready before processing servers
        var allBotsReady = false;
        while (!allBotsReady && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                allBotsReady = true;
                foreach (var bot in botManager.GetAllBots())
                {
                    if (bot.Client.ConnectionState != ConnectionState.Connected)
                    {
                        allBotsReady = false;
                        break;
                    }
                }

                if (!allBotsReady)
                {
                    logger.LogInformation("Waiting for Discord clients to connect...");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            catch (Exception)
            {
                // If botManager isn't ready yet or has no bots, wait and try again
                allBotsReady = false;
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessServersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Assetto servers");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessServersAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Processing Assetto servers");

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var servers = await dbContext.AssettoServers
            .Include(s => s.StatusMessages)
            .Where(s => s.IsActive)
            .ToListAsync(stoppingToken);

        foreach (var server in servers)
            try
            {
                await ProcessServerAsync(server, dbContext, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing server {ServerId} at {ServerUrl}", server.Id, server.ApiUrl);
            }
    }

    private async Task ProcessServerAsync(AssettoServerEntity server, ApplicationDbContext dbContext,
        CancellationToken stoppingToken)
    {
        var client = clientFactory.CreateClient(server.ApiUrl);

        try
        {
            DetailResponse? details = null;

            try
            {
                details = await client.GetServerDetailsAsync();
            }
            catch
            {
                // ignore connection errors, server might be offline
            }

            // Update server last checked time
            server.LastChecked = DateTime.UtcNow;

            // Update all status messages for this server
            foreach (var statusMessage in server.StatusMessages)
            {
                // Find the appropriate bot for this status message
                // Use the bot manager to determine which bot should be used for this server/guild
                var botInstance = botManager.GetBotForGuild(statusMessage.GuildId);
                
                await messageGenerator.UpdateServerStatusMessageAsync(statusMessage, details, botInstance.Client);
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing server {ServerId} at {ServerUrl}", server.Id, server.ApiUrl);
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}