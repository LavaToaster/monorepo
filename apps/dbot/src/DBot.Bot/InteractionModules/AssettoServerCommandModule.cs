using DBot.Bot.Interfaces;
using Discord;
using Discord.Interactions;
using Refit;

namespace DBot.Bot.InteractionModules;

/// <summary>
///     Discord interaction commands for managing Assetto Corsa servers
/// </summary>
[DefaultMemberPermissions(GuildPermission.Administrator)]
[CommandContextType(InteractionContextType.Guild)]
[Group("server", "Commands for interacting with Assetto Corsa servers")]
public class AssettoServerCommandModule(
    IAssettoServerService serverService,
    ILogger<AssettoServerCommandModule> logger)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("add", "Add or update an Assetto Corsa server")]
    public async Task AddServerAsync(
        [Summary("name", "Friendly name for this server")]
        string displayName,
        [Summary("host", "Server hostname or IP")]
        string host,
        [Summary("port", "Server http port")] int port)
    {
        await DeferAsync(true);

        try
        {
            var guildId = Context.Guild.Id;
            var result = await serverService.AddOrUpdateServerAsync(guildId, displayName, host, port);

            var embed = new EmbedBuilder()
                .WithTitle(result.IsNewServer ? "🆕 Server Added" : "✅ Server Updated")
                .WithDescription(
                    $"Server **{displayName}** has been {(result.IsNewServer ? "added" : "updated")}.")
                .WithColor(result.IsNewServer ? Color.Green : Color.Blue)
                .AddField("Server URL", result.Server.ApiUrl)
                .Build();

            await FollowupAsync(embed: embed, ephemeral: true);
        }
        catch (ApiException exception)
        {
            logger.LogError("Unable to connect to server {Host}:{Port} - {Message}", host, port, exception.Message);

            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Error")
                .WithDescription(
                    $"Failed to connect to the server at {host}:{port}. Please verify the server is online and the address is correct.")
                .WithColor(Color.Red)
                .Build();

            await FollowupAsync(embed: errorEmbed, ephemeral: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add/update server {DisplayName} at {Host}:{Port}", displayName, host, port);

            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Error")
                .WithDescription(
                    $"Failed to connect to the server at {host}:{port}. Please verify the server is online and the address is correct.")
                .WithColor(Color.Red)
                .Build();

            await FollowupAsync(embed: errorEmbed, ephemeral: true);
        }
    }

    [SlashCommand("list", "List all configured servers")]
    public async Task ListServersAsync()
    {
        await DeferAsync();

        try
        {
            var guildId = Context.Guild.Id;
            var servers = await serverService.GetGuildServersAsync(guildId);

            if (servers.Count == 0)
            {
                await FollowupAsync("No Assetto Corsa servers have been configured yet. Use `/server add` to add one.",
                    ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🏎️ Configured Servers")
                .WithColor(Color.Blue);

            foreach (var server in servers)
            {
                var serverDetails = await serverService.GetServerByIdAsync(server.AssettoServerEntityId);
                embed.AddField(server.DisplayName, $"ID: {server.Id}\nURL: {serverDetails?.ApiUrl ?? "Unknown"}");
            }

            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list servers");
            await FollowupAsync("⚠️ An error occurred while retrieving the server list.", ephemeral: true);
        }
    }
}