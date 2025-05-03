using DBot.Core.Data.Context;
using DBot.Core.Data.Entities;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace DBot.Bot.Services;

public class RoleMirrorService(
    DiscordBotManager botManager,
    ILogger<RoleMirrorService> logger,
    ApplicationDbContext context)
{
    public async Task RegisterRoleCandidateAsync(ulong guildId, ulong roleId)
    {
        var roleMirror = new RoleMirrorCandidateEntity
        {
            GuildId = guildId,
            RoleId = roleId
        };

        context.RoleMirrorCandidates.Add(roleMirror);
        await context.SaveChangesAsync();
    }
    
    public async Task UnregisterRoleCandidateAsync(ulong guildId, ulong roleId)
    {
        var roleMirror = await context.RoleMirrorCandidates
            .Include(r => r.SourceRoleConfigurations)
            .Include(r => r.TargetRoleConfigurations)
            .FirstAsync(r => r.GuildId == guildId && r.RoleId == roleId);

        if (roleMirror.SourceRoleConfigurations.Count > 0 ||
            roleMirror.TargetRoleConfigurations.Count > 0)
        {
            throw new InvalidOperationException("Cannot unregister a role that still has mappings.");
        }
        
        context.RoleMirrorCandidates.Remove(roleMirror);
        await context.SaveChangesAsync();
    }
    
    public async Task<List<RoleMirrorCandidateEntity>> GetAllRoleCandidatesAsync(ulong guildId)
    {
        return await context.RoleMirrorCandidates
            .Where(r => r.GuildId == guildId)
            .ToListAsync();
    }

    public async Task<RoleMirrorCandidateEntity?> GetRoleCandidateAsync(Guid id)
    {
        return await context.RoleMirrorCandidates
            .FirstOrDefaultAsync(r => r.Id == id);
    }
    
    public async Task<RoleMirrorCandidateEntity?> GetRoleCandidateAsync(ulong guildId, ulong roleId)
    {
        return await context.RoleMirrorCandidates
            .FirstOrDefaultAsync(r => r.GuildId == guildId && r.RoleId == roleId);
    }

    public async Task RegisterRoleMappingAsync(RoleMirrorCandidateEntity source, RoleMirrorCandidateEntity target, RoleSync syncMode)
    {
        if (source.Id == target.Id)
        {
            throw new InvalidOperationException("Source and target roles must be different.");
        }
    
        var mapping = new RoleMirrorMappingEntity
        {
            SourceRoleId = source.Id,
            TargetRoleId = target.Id,
            SyncMode = syncMode
        };

        context.RoleMirrorMappings.Add(mapping);
        await context.SaveChangesAsync();
    }
    
    public async Task UnregisterRoleMappingAsync(RoleMirrorMappingEntity mapping)
    {
        context.RoleMirrorMappings.Remove(mapping);
        await context.SaveChangesAsync();
    }
    
    public async Task UpdateUserRole(ulong guildId, SocketGuildUser user, ulong[] addedRoles, ulong[] removedRoles)
    {
        var roleMirrorMappings = await context.RoleMirrorMappings
            .Include(r => r.SourceRole)
            .Include(r => r.TargetRole)
            .Where(r => r.SourceRole.GuildId == guildId &&
                        (addedRoles.Contains(r.SourceRole.RoleId) || removedRoles.Contains(r.SourceRole.RoleId)))
            .ToArrayAsync();
        
        var rolesToRemove = roleMirrorMappings
            .Where(r => removedRoles.Contains(r.SourceRole.RoleId))
            .ToArray();
        
        var rolesToAdd = roleMirrorMappings
            .Where(r => addedRoles.Contains(r.SourceRole.RoleId))
            .ToArray();
        
        foreach (var roleMirrorMapping in rolesToRemove)
        {
            var targetRole = roleMirrorMapping.TargetRole;
            var botInstance = botManager.GetBotForGuild(targetRole.GuildId);
            if (botInstance == null) continue;
            
            var guild = botInstance.Client.GetGuild(targetRole.GuildId);
            var guildUser = guild?.GetUser(user.Id);
            
            if (guildUser == null)
            {
                // User not found in the target guild
                continue;
            }
            
            var role = guild.GetRole(targetRole.RoleId);
            if (role != null)
            {
                await guildUser.RemoveRoleAsync(role);
            }
            
            logger.LogInformation("Removed role {RoleId} from user {UserId} in guild {GuildId}", targetRole.RoleId, user.Id, targetRole.GuildId);
        }
        
        foreach (var roleMirrorMapping in rolesToAdd)
        {
            var targetRole = roleMirrorMapping.TargetRole;
            var botInstance = botManager.GetBotForGuild(targetRole.GuildId);
            if (botInstance == null) continue;
            
            var guild = botInstance.Client.GetGuild(targetRole.GuildId);
            var guildUser = guild?.GetUser(user.Id);
            
            if (guildUser == null)
            {
                // User not found in the target guild
                continue;
            }
            
            var role = guild.GetRole(targetRole.RoleId);
            if (role != null)
            {
                await guildUser.AddRoleAsync(role);
            }
            
            logger.LogInformation("Added role {RoleId} to user {UserId} in guild {GuildId}", targetRole.RoleId, user.Id, targetRole.GuildId);
        }
    }
}