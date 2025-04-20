using DBot.Bot.Interfaces;
using DBot.Bot.Models.Assetto;
using DBot.Core.Data.Context;
using DBot.Core.Data.Entities;
using DBot.Integrations.Assetto;
using Microsoft.EntityFrameworkCore;

namespace DBot.Bot.Services;

/// <summary>
///     Service for managing Assetto Corsa servers
/// </summary>
public class AssettoServerService(
    ApplicationDbContext dbContext,
    AssettoServerClientFactory clientFactory,
    ILogger<AssettoServerService> logger)
    : IAssettoServerService
{
    public async Task<AddServerResult> AddOrUpdateServerAsync(ulong guildId, string displayName, string host, int port)
    {
        var apiUrl = $"http://{host}:{port}";

        // Try to connect to the server first to validate it's an Assetto server
        var client = clientFactory.CreateClient(apiUrl);
        await client.GetServerDetailsAsync(); // Will throw if server is unreachable or invalid

        // Check if server already exists by URL
        var existingServer = await dbContext.AssettoServers
            .Include(s => s.GuildConfigurations)
            .FirstOrDefaultAsync(s => s.ApiUrl == apiUrl);

        AssettoServerEntity serverEntity;
        var isNewServer = false;

        if (existingServer == null)
        {
            // Create new server
            serverEntity = new AssettoServerEntity
            {
                ApiUrl = apiUrl,
                IsActive = true,
                LastChecked = DateTime.UtcNow
            };

            dbContext.AssettoServers.Add(serverEntity);
            isNewServer = true;
            logger.LogInformation("Adding new Assetto server with URL {ApiUrl}", apiUrl);
        }
        else
        {
            // Use existing server
            serverEntity = existingServer;
            serverEntity.LastChecked = DateTime.UtcNow;
            logger.LogInformation("Found existing Assetto server with URL {ApiUrl}", apiUrl);
        }

        // Check if guild configuration exists for this server
        AssettoServerGuildEntity? guildConfig = null;

        if (!isNewServer)
            guildConfig = serverEntity.GuildConfigurations
                .FirstOrDefault(gc => gc.GuildId == guildId);

        if (guildConfig == null)
        {
            // Create new guild configuration
            guildConfig = new AssettoServerGuildEntity
            {
                GuildId = guildId,
                DisplayName = displayName,
                AssettoServerEntityId = serverEntity.Id
            };

            dbContext.GuildConfigurations.Add(guildConfig);
            logger.LogInformation("Adding guild configuration for guild {GuildId}", guildId);
        }
        else
        {
            // Update existing guild configuration
            guildConfig.DisplayName = displayName;
            logger.LogInformation("Updating guild configuration for guild {GuildId}", guildId);
        }

        await dbContext.SaveChangesAsync();

        return new AddServerResult
        {
            Server = serverEntity,
            GuildConfiguration = guildConfig,
            IsNewServer = isNewServer
        };
    }

    public async Task<List<AssettoServerGuildEntity>> GetGuildServersAsync(ulong guildId)
    {
        var guildConfigs = await dbContext.GuildConfigurations
            .Where(gc => gc.GuildId == guildId)
            .ToListAsync();

        return guildConfigs.ToList();
    }

    public async Task<AssettoServerEntity?> GetServerByIdAsync(Guid serverId)
    {
        var server = await dbContext.AssettoServers
            .Include(s => s.GuildConfigurations)
            .FirstOrDefaultAsync(s => s.Id == serverId);

        return server;
    }

    public async Task<AssettoServerGuildEntity?> GetGuildServerAsync(Guid serverId, ulong guildId)
    {
        var guildConfig = await dbContext.GuildConfigurations
            .FirstOrDefaultAsync(gc => gc.AssettoServerEntityId == serverId && gc.GuildId == guildId);

        return guildConfig;
    }

    public async Task<AssettoServerMonitorEntity?> GetStatusMessageAsync(ulong messageId)
    {
        var statusMessage = await dbContext.StatusMessages
            .Include(sm => sm.ServerEntity) // Eager load the associated server
            .FirstOrDefaultAsync(sm => sm.MessageId == messageId);

        return statusMessage;
    }

    public async Task AddStatusMessageAsync(AssettoServerMonitorEntity monitor)
    {
        dbContext.StatusMessages.Add(monitor);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Added status message {MessageId} for server {ServerId}",
            monitor.MessageId, monitor.ServerEntityId);
    }

    public async Task UpdateStatusMessageAsync(AssettoServerMonitorEntity monitor)
    {
        var entity = await dbContext.StatusMessages
            .FirstOrDefaultAsync(sm => sm.MessageId == monitor.MessageId);

        if (entity == null)
            throw new InvalidOperationException($"Status message with ID {monitor.MessageId} not found");

        entity.Description = monitor.Description;
        entity.ThumbnailUrl = monitor.ThumbnailUrl;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Updated status message {MessageId} for server {ServerId}",
            monitor.MessageId, monitor.ServerEntityId);
    }

    public async Task RemoveStatusMessageAsync(AssettoServerMonitorEntity monitor)
    {
        var entity = await dbContext.StatusMessages
            .FirstOrDefaultAsync(sm => sm.MessageId == monitor.MessageId);

        if (entity == null)
            throw new InvalidOperationException($"Status message with ID {monitor.MessageId} not found");

        dbContext.StatusMessages.Remove(entity);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Removed status message {MessageId} for server {ServerId}",
            monitor.MessageId, monitor.ServerEntityId);
    }
}