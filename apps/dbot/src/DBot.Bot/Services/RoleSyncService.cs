using DBot.Core.Data.Context;
using DBot.Core.Data.Entities;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DBot.Bot.Services;

/// <summary>
/// Service to synchronize role mappings between source and target servers.
/// </summary>
public class RoleSyncService(
    DiscordSocketClient client,
    ILogger<RoleSyncService> logger,
    ApplicationDbContext context)
{
    /// <summary>
    /// Synchronizes all role mappings from source servers to target servers
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task SyncAllRoleMappingsAsync(ulong[] guildIds)
    {
        var allMappings = await context.RoleMirrorMappings
            .Include(m => m.SourceRole)
            .Include(m => m.TargetRole)
            .Where(m => guildIds.Length == 0 || guildIds.Contains(m.TargetRole.GuildId))
            .ToListAsync();
            
        foreach (var mapping in allMappings)
        {
            await SyncRoleMappingAsync(mapping);
        }
    }
    
    public Task SyncAllRoleMappingsAsync(ulong guildId)
    {
        return SyncAllRoleMappingsAsync([guildId]);
    }

    /// <summary>
    /// Synchronizes a specific role mapping from source to target
    /// </summary>
    /// <param name="mapping">The role mapping to synchronize</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task SyncRoleMappingAsync(RoleMirrorMappingEntity mapping)
    {
        try
        {
            var sourceGuild = client.GetGuild(mapping.SourceRole.GuildId);
            var targetGuild = client.GetGuild(mapping.TargetRole.GuildId);

            if (sourceGuild == null || targetGuild == null)
            {
                logger.LogWarning("Could not sync roles - guild not found. SourceGuildId: {SourceGuildId}, TargetGuildId: {TargetGuildId}",
                    mapping.SourceRole.GuildId, mapping.TargetRole.GuildId);
                return;
            }

            var sourceRole = sourceGuild.GetRole(mapping.SourceRole.RoleId);
            var targetRole = targetGuild.GetRole(mapping.TargetRole.RoleId);

            if (sourceRole == null || targetRole == null)
            {
                logger.LogWarning("Could not sync roles - role not found. SourceRoleId: {SourceRoleId}, TargetRoleId: {TargetRoleId}",
                    mapping.SourceRole.RoleId, mapping.TargetRole.RoleId);
                return;
            }

            // Get all users with the source role
            var sourceUsers = sourceGuild.Users.Where(u => u.Roles.Any(r => r.Id == sourceRole.Id)).ToList();

            // Process all users based on sync mode
            foreach (var sourceUser in sourceUsers)
            {
                // Try to find the user in the target guild
                var targetUser = targetGuild.Users.FirstOrDefault(u => u.Id == sourceUser.Id);
                if (targetUser == null) continue;

                // Add the target role if user doesn't have it
                if (targetUser.Roles.Any(r => r.Id == targetRole.Id)) continue;
                
                await targetUser.AddRoleAsync(targetRole);
                logger.LogInformation("Added role {RoleName} to user {Username} in guild {GuildName}",
                    targetRole.Name, targetUser.Username, targetGuild.Name);
            }

            // For strict sync mode, we also need to remove roles from users who don't have the source role
            if (mapping.SyncMode == RoleSync.Strict)
            {
                var targetUsers = targetGuild.Users.Where(u => u.Roles.Any(r => r.Id == targetRole.Id)).ToList();

                foreach (var targetUser in targetUsers)
                {
                    // Check if user exists in source guild and has the source role
                    var sourceUser = sourceGuild.Users.FirstOrDefault(u => u.Id == targetUser.Id);
                    var shouldHaveRole = sourceUser != null && sourceUser.Roles.Any(r => r.Id == sourceRole.Id);

                    // Remove the role if the user shouldn't have it according to strict sync
                    if (shouldHaveRole) continue;
                    
                    await targetUser.RemoveRoleAsync(targetRole);
                    logger.LogInformation("Removed role {RoleName} from user {Username} in guild {GuildName} (strict sync)",
                        targetRole.Name, targetUser.Username, targetGuild.Name);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error syncing role mapping: {Message}", ex.Message);
        }
    }
}
