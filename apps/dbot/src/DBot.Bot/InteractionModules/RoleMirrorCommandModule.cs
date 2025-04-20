using System.ComponentModel;
using DBot.Bot.Embeds;
using DBot.Bot.InteractionModules.Autocomplete;
using DBot.Bot.Services;
using DBot.Core.Data.Entities;
using Discord;
using Discord.Interactions;

namespace DBot.Bot.InteractionModules;

[Group("mirror", "Role commands")]
public class RoleMirrorCommandModule(ILogger<RoleMirrorCommandModule> logger, RoleMirrorService service, RoleSyncService syncService)
     : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("register", "Register a role to be mirrored")]
    public async Task RegisterRoleAsync(
        [Description("Role to be mirrored")] IRole role)
    {
        await DeferAsync(true);

        try
        {
            await service.RegisterRoleCandidateAsync(Context.Guild.Id, role.Id);
            
            var embed = StatusEmbedGenerator.Success("Role Registered",
                $"Role **{role.Name}** has been registered for mirroring.");
            
            await FollowupAsync(embed: embed, ephemeral: true);
        }
        catch (Exception ex)
        {
            var errorEmbed = StatusEmbedGenerator.Error($"Failed to register role: {ex.Message}");
            
            await FollowupAsync(embed: errorEmbed, ephemeral: true);
        }
    }
    
    [SlashCommand("unregister", "Unregister a role from being mirrored")]
    public async Task UnregisterRoleAsync(
        [Description("Role to be unregistered")] IRole role)
    {
        await DeferAsync(true);

        try
        {
            await service.UnregisterRoleCandidateAsync(Context.Guild.Id, role.Id);
            
            var embed = StatusEmbedGenerator.Success("Role Unregistered",
                $"Role **{role.Name}** has been unregistered from mirroring.");
            
            await FollowupAsync(embed: embed, ephemeral: true);
        }
        catch (Exception ex)
        {
            var errorEmbed = StatusEmbedGenerator.Error($"Failed to unregister role: {ex.Message}");
            
            await FollowupAsync(embed: errorEmbed, ephemeral: true);
        }
    }
    
    [SlashCommand("list", "List all registered roles")]
    public async Task ListRolesAsync()
    {
        await DeferAsync(true);

        try
        {
            var roles = await service.GetAllRoleCandidatesAsync(Context.Guild.Id);
            
            if (roles.Count == 0)
            {
                await FollowupAsync("No roles registered for mirroring.", ephemeral: true);
                return;
            }

            var roleDescriptions = roles.Select(r => $"<@&{r.RoleId}>");
            
            var embed = new EmbedBuilder()
                .WithTitle("Registered Roles")
                .WithDescription(string.Join("\n", roleDescriptions))
                .WithColor(Color.Blue)
                .Build();
            
            await FollowupAsync(embed: embed, ephemeral: true);
        }
        catch (Exception ex)
        {
            var errorEmbed = StatusEmbedGenerator.Error($"Failed to list roles: {ex.Message}");
            
            await FollowupAsync(embed: errorEmbed, ephemeral: true);
        }
    }
    
    [SlashCommand("map", "Map two roles for mirroring")]
    public async Task MapRolesAsync(
        [Description("Source role to mirror from")]
        [Autocomplete(typeof(RoleMirrorSourceRoleAutocompleteHandler))]
        string sourceRoleInput,
        [Description("Target role to mirror to")]
        [Autocomplete(typeof(RoleMirrorTargetRoleAutocompleteHandler))]
        string targetRoleInput,
        [Description("Sync mode")]
        [Choice("Strict - ensures that target roles exactly match source roles, removing any roles not in the mapping", (int)RoleSync.Strict)]
        [Choice("Preserve - adds mapped roles but doesn't remove other roles from the target", (int)RoleSync.Preserve)]
        int syncMode)
    {
        await DeferAsync(true);
    
        try
        {
            var sourceRole = await service.GetRoleCandidateAsync(Guid.Parse(sourceRoleInput));
            var targetRole = await service.GetRoleCandidateAsync(Guid.Parse(targetRoleInput));
            
            if (sourceRole == null || targetRole == null)
            {
                var errorEmbed = StatusEmbedGenerator.Error("Invalid roles provided for mapping.");
                await FollowupAsync(embed: errorEmbed, ephemeral: true);
                return;
            }

            var syncModeRole = (RoleSync)syncMode;

            await service.RegisterRoleMappingAsync(sourceRole, targetRole, syncModeRole);
            
            var sourceGuild = Context.Client.GetGuild(sourceRole.GuildId);
            var sourceRoleName = sourceGuild.GetRole(sourceRole.RoleId).Name;
            var targetRoleName = Context.Guild.GetRole(targetRole.RoleId).Name;

            var embed = new EmbedBuilder()
                .WithTitle("Roles Mapped") 
                .AddField($"Source Role ({sourceGuild.Name})", sourceRoleName, true)
                .AddField($"Target Role ({Context.Guild.Name})", targetRoleName, true)
                .AddField("Sync Mode", syncMode.ToString(), true)
                .WithColor(Color.Green)
                .Build();
            
            await FollowupAsync(embed: embed, ephemeral: true);
        }
        catch (Exception ex)
        {
            var errorEmbed = StatusEmbedGenerator.Error($"Failed to map roles: {ex.Message}");
            
            await FollowupAsync(embed: errorEmbed, ephemeral: true);
        }
    }

    [SlashCommand("sync", "Sync roles between source and target")]
    public async Task SyncRolesAsync()
    {
        await DeferAsync(true);

        try
        { 
            await syncService.SyncAllRoleMappingsAsync(Context.Guild.Id);
            
            var embed = StatusEmbedGenerator.Success("Roles Synced", "All roles have been synced successfully.");
            
            await FollowupAsync(embed: embed, ephemeral: true);
        }
        catch (Exception ex)
        {
            var errorEmbed = StatusEmbedGenerator.Error($"Failed to sync roles: {ex.Message}");
            
            await FollowupAsync(embed: errorEmbed, ephemeral: true);
        }
    }
}