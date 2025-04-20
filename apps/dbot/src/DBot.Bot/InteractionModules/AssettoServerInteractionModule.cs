using DBot.Bot.Interfaces;
using DBot.Bot.Modals;
using DBot.Bot.Services;
using DBot.Core.Data.Entities;
using Discord;
using Discord.Interactions;
using Refit;

namespace DBot.Bot.InteractionModules;

// Modal class to handle the server details inputs

[DefaultMemberPermissions(GuildPermission.Administrator)]
[CommandContextType(InteractionContextType.Guild)]
public class AssettoServerInteractionModule(
    IAssettoServerService serverService,
    ILogger<AssettoServerInteractionModule> logger,
    AssettoStatusMessageGenerator messageGenerator)
    : InteractionModuleBase<SocketInteractionContext>
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [MessageCommand("Configure Assetto Server Monitor")]
    public async Task TrackMessageAsync(IMessage message)
    {
        await DeferAsync(true);

        try
        {
            var guildServers = await serverService.GetGuildServersAsync(Context.Guild.Id);

            if (!guildServers.Any())
            {
                await FollowupAsync(
                    "You haven't added any Assetto Corsa servers to this guild yet. Use `/server add` to add a server first.",
                    ephemeral: true);
                return;
            }

            var existingStatusMessage = await serverService.GetStatusMessageAsync(message.Id);

            if (existingStatusMessage != null)
            {
                var removeComponent = new ComponentBuilder()
                    .WithButton("Edit Existing Monitor", $"edit-monitor:{message.Id}", ButtonStyle.Secondary)
                    .WithButton("Remove Existing Monitor", $"remove-monitor:{message.Id}", ButtonStyle.Danger)
                    .Build();

                await FollowupAsync(
                    "This message is already tracking a server. You can either edit the existing monitor or remove it.",
                    components: removeComponent,
                    ephemeral: true);

                return;
            }

            var options = guildServers.Select(gs => new SelectMenuOptionBuilder()
                .WithLabel(gs.DisplayName)
                .WithValue(gs.AssettoServerEntityId.ToString())).ToList();

            var selectMenu = new SelectMenuBuilder()
                .WithCustomId($"server-select:{message.Id}")
                .WithPlaceholder("Select a server to track")
                .WithOptions(options);

            var component = new ComponentBuilder()
                .WithSelectMenu(selectMenu)
                .Build();

            await FollowupAsync("Please select which server to monitor:", components: component, ephemeral: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating server selection menu");
            await FollowupAsync("An error occurred while creating the server selection menu. Please try again later.",
                ephemeral: true);
        }
    }

    [ComponentInteraction("server-select:*")]
    public async Task HandleServerSelectAsync(string messageId, string[] selected)
    {
        try
        {
            if (!Guid.TryParse(selected[0], out var serverId))
            {
                await RespondAsync("Invalid server selection. Please try again.", ephemeral: true);
                return;
            }

            // Create a modal for the inputs
            var modal = new ModalBuilder()
                .WithTitle("Server Status Details")
                .WithCustomId($"server-details:{messageId}:{serverId}")
                .AddTextInput("Description", "description", TextInputStyle.Paragraph,
                    "Enter a custom description for this server status (optional)",
                    required: false,
                    maxLength: 1000)
                .AddTextInput("Thumbnail URL", "thumbnail", TextInputStyle.Short,
                    "Enter a URL for the server thumbnail image (optional)",
                    required: false,
                    maxLength: 500)
                .Build();

            // Respond with the modal
            await RespondWithModalAsync(modal);
            await DeleteOriginalResponseAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling server selection");
            await RespondAsync("An error occurred while processing your selection. Please try again later.",
                ephemeral: true);
        }
    }

    [ModalInteraction("server-details:*:*")]
    public async Task HandleServerDetailsModalAsync(string messageId, string serverId, ServerDetailsModal modal)
    {
        await DeferAsync(true);

        try
        {
            if (!Guid.TryParse(serverId, out var serverIdGuid))
            {
                await FollowupAsync("Invalid server selection. Please try again.", ephemeral: true);
                return;
            }

            var server = await serverService.GetServerByIdAsync(serverIdGuid);

            if (server == null)
            {
                await FollowupAsync("Selected server was not found. Please try again.", ephemeral: true);
                return;
            }

            var statusMessage = new AssettoServerMonitorEntity
            {
                GuildId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                MessageId = ulong.Parse(messageId),
                ServerEntityId = server.Id,
                Description = modal.Description,
                ThumbnailUrl = modal.ThumbnailUrl
            };

            try
            {
                await messageGenerator.CreateServerStatusMessageAsync(statusMessage, server);
            }
            catch (ApiException ex)
            {
                logger.LogError(ex, "Failed to connect to Assetto server {ServerId}", serverIdGuid);
                await FollowupAsync(
                    "Failed to connect to the selected server. Please verify the server is online and try again.",
                    ephemeral: true);
                return;
            }

            await serverService.AddStatusMessageAsync(statusMessage);

            var guildServer = await serverService.GetGuildServerAsync(serverIdGuid, Context.Guild.Id);

            await FollowupAsync(
                $"Message will now track the status of server **{guildServer?.DisplayName ?? "Unknown"}**!",
                ephemeral: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing server details");
            await FollowupAsync("An error occurred while setting up server tracking. Please try again later.",
                ephemeral: true);
        }
    }

    [ComponentInteraction("remove-monitor:*")]
    public async Task HandleRemovemonitorAsync(string messageId)
    {
        await DeferAsync(true);

        try
        {
            var statusMessage = await serverService.GetStatusMessageAsync(ulong.Parse(messageId));

            if (statusMessage == null)
            {
                await FollowupAsync("Could not find status monitor to remove.", ephemeral: true);
                return;
            }

            if (await Context.Channel.GetMessageAsync(ulong.Parse(messageId)) is IUserMessage message)
                await message.ModifyAsync(m =>
                {
                    m.Embed = null;
                    m.Embeds = new Optional<Embed[]>([]);

                    m.Content = message.Content;
                    if (m.Components.IsSpecified) m.Components = m.Components;
                    if (m.Attachments.IsSpecified) m.Attachments = m.Attachments;
                });

            await serverService.RemoveStatusMessageAsync(statusMessage);
            await DeleteOriginalResponseAsync();

            await FollowupAsync("Status monitor removed! You can now track a different server on this message.",
                ephemeral: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing status monitor");
            await FollowupAsync("An error occurred while removing the monitor. Please try again later.",
                ephemeral: true);
        }
    }

    [ComponentInteraction("edit-monitor:*")]
    public async Task HandleEditMonitorAsync(string messageId)
    {
        try
        {
            var statusMessage = await serverService.GetStatusMessageAsync(ulong.Parse(messageId));

            if (statusMessage == null)
            {
                await RespondAsync("Could not find status monitor to edit.", ephemeral: true);
                return;
            }

            var modal = new ModalBuilder()
                .WithTitle("Edit Server Status Details")
                .WithCustomId($"edit-monitor-details:{messageId}")
                .AddTextInput("Description", "description", TextInputStyle.Paragraph,
                    "Enter a custom description for this server status (optional)",
                    required: false,
                    maxLength: 1000,
                    value: statusMessage.Description)
                .AddTextInput("Thumbnail URL", "thumbnail", TextInputStyle.Short,
                    "Enter a URL for the server thumbnail image (optional)",
                    required: false,
                    maxLength: 500,
                    value: statusMessage.ThumbnailUrl)
                .Build();

            await RespondWithModalAsync(modal);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling edit monitor");
            await RespondAsync("An error occurred while processing your request. Please try again later.",
                ephemeral: true);
        }
    }

    [ModalInteraction("edit-monitor-details:*")]
    public async Task HandleEditServerDetailsModalAsync(string messageId, ServerDetailsModal modal)
    {
        await DeferAsync(true);

        try
        {
            var statusMessage = await serverService.GetStatusMessageAsync(ulong.Parse(messageId));

            if (statusMessage == null)
            {
                await FollowupAsync("Could not find status monitor to edit.", ephemeral: true);
                return;
            }

            statusMessage.Description = modal.Description;
            statusMessage.ThumbnailUrl = modal.ThumbnailUrl;

            await serverService.UpdateStatusMessageAsync(statusMessage);
            await DeleteOriginalResponseAsync();

            try
            {
                // Update the embed with the new details
                logger.LogInformation("Updating server status message with new details");
                await messageGenerator.CreateServerStatusMessageAsync(statusMessage, statusMessage.ServerEntity);
                logger.LogInformation("Server status message updated successfully");
            }
            catch
            {
                // ignored, will be picked up by the monitor
            }

            await FollowupAsync("Status monitor updated successfully!", ephemeral: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating server details");
            await FollowupAsync("An error occurred while updating the monitor. Please try again later.",
                ephemeral: true);
        }
    }
}