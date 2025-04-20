using DBot.Bot.Models.Assetto;
using DBot.Core.Data.Entities;

namespace DBot.Bot.Interfaces;

/// <summary>
///     Service interface for managing Assetto Corsa servers
/// </summary>
public interface IAssettoServerService
{
    /// <summary>
    ///     Adds or updates an Assetto Corsa server for a guild
    /// </summary>
    Task<AddServerResult> AddOrUpdateServerAsync(ulong guildId, string displayName, string host, int port);

    /// <summary>
    ///     Gets all servers configured for a specific guild
    /// </summary>
    Task<List<AssettoServerGuildEntity>> GetGuildServersAsync(ulong guildId);

    /// <summary>
    ///     Gets a server by its ID
    /// </summary>
    Task<AssettoServerEntity?> GetServerByIdAsync(Guid serverId);

    /// <summary>
    ///     Gets a guild-specific server configuration
    /// </summary>
    Task<AssettoServerGuildEntity?> GetGuildServerAsync(Guid serverId, ulong guildId);

    /// <summary>
    ///     Gets a status message by its Discord message ID
    /// </summary>
    Task<AssettoServerMonitorEntity?> GetStatusMessageAsync(ulong messageId);

    /// <summary>
    ///     Adds a new status message to track a server
    /// </summary>
    Task AddStatusMessageAsync(AssettoServerMonitorEntity monitor);

    /// <summary>
    ///     Updates an existing status message
    /// </summary>
    Task UpdateStatusMessageAsync(AssettoServerMonitorEntity monitor);

    /// <summary>
    ///     Removes a status message
    /// </summary>
    Task RemoveStatusMessageAsync(AssettoServerMonitorEntity monitor);
}