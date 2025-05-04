using DBot.Bot.Embeds;
using Discord;
using Discord.Interactions;

namespace DBot.Bot.InteractionModules;

[DefaultMemberPermissions(GuildPermission.Administrator)]
public class MessageInteractionsModule(ILogger<MessageInteractionsModule> logger) : InteractionModuleBase<SocketInteractionContext>
{
    [MessageCommand("Edit Message")]
    public async Task EditMessage(IMessage message)
    {
        if (message.Author.Id != Context.Client.CurrentUser.Id)
        {
            var embed = StatusEmbedGenerator.Error($"You can only edit messages sent by {Context.Guild.CurrentUser.DisplayName}.");
            
            await RespondAsync(embed: embed, ephemeral: true);
            return;
        }
        
        var mb = new ModalBuilder()
            .WithTitle("Edit Message")
            .WithCustomId("edit-message-modal:" + message.Id);

        // If Message is ThreadMessage
        if (message is IThreadChannel thread) mb.AddTextInput("Thread Title", "threadTitle", value: thread.Name);

        mb.AddTextInput("Message", "message", TextInputStyle.Paragraph, value: message.Content);

        await RespondWithModalAsync(mb.Build());
    }

    [ModalInteraction("edit-message-modal:*")]
    public async Task HandleEditMessageModal(string messageId, EditMessageModal modal)
    {
        if (await Context.Channel.GetMessageAsync(ulong.Parse(messageId)) is not IUserMessage message)
        {
            await RespondAsync("Couldn't find the message to update", ephemeral: true);
            return;
        }

        await message.ModifyAsync(m =>
        {
            m.Content = modal.Message;

            if (m.Embed.IsSpecified) m.Embed = m.Embed;
            if (m.Embeds.IsSpecified) m.Embeds = m.Embeds;
            if (m.Components.IsSpecified) m.Components = m.Components;
            if (m.Attachments.IsSpecified) m.Attachments = m.Attachments;
        });

        await RespondAsync($"Message updated: {modal.Message}", ephemeral: true);
    }

    public class EditMessageModal : IModal
    {
        [InputLabel("ThreadTitle")]
        [ModalTextInput("threadTitle")]
        public string ThreadTitle { get; set; } = "";

        [InputLabel("Message")]
        [ModalTextInput("message", TextInputStyle.Paragraph)]
        public string Message { get; set; } = "";

        public string Title => "Edit Message";
    }

    [MessageCommand("Add link component")]
    public async Task AddLinkComponent(IMessage message)
    {
        if (message.Author.Id != Context.Client.CurrentUser.Id)
        {
            var embed = StatusEmbedGenerator.Error($"You can only add components to messages sent by {Context.Guild.CurrentUser.DisplayName}.");
        
            await RespondAsync(embed: embed, ephemeral: true);
            return;
        }

        var mb = new ModalBuilder()
            .WithTitle("Add Link Component")
            .WithCustomId("add-link-component-modal:" + message.Id);
        
        mb.AddTextInput("URL", "url", required: true, placeholder: "https://example.com");
        mb.AddTextInput("Label", "label", required: true, placeholder: "Click me!");
        mb.AddTextInput("Emoji ID", "emojiId", required: false, placeholder: "Discord emoji ID");

        await RespondWithModalAsync(mb.Build());
    }

    [ModalInteraction("add-link-component-modal:*")]
    public async Task HandleAddLinkComponentModal(string messageId, AddLinkComponentModal modal)
    {
        await DeferAsync(true);

        try
        {
            if (await Context.Channel.GetMessageAsync(ulong.Parse(messageId)) is not IUserMessage message)
            {
                await FollowupAsync("Couldn't find the message to update", ephemeral: true);
                return;
            }

            // Create the component
            var componentBuilder = new ComponentBuilder();
            var buttonBuilder = new ButtonBuilder()
                .WithStyle(ButtonStyle.Link)
                .WithUrl(modal.Url)
                .WithLabel(modal.Label);

            // Add emoji if provided
            if (!string.IsNullOrWhiteSpace(modal.EmojiId) && ulong.TryParse(modal.EmojiId, out var emojiId))
            {
                buttonBuilder.WithEmote(new Emote(emojiId, "", false));
            }

            componentBuilder.WithButton(buttonBuilder);

            await message.ModifyAsync(m =>
            {
                m.Components = componentBuilder.Build();

                // Preserve other properties
                if (m.Content.IsSpecified) m.Content = m.Content;
                if (m.Embed.IsSpecified) m.Embed = m.Embed;
                if (m.Embeds.IsSpecified) m.Embeds = m.Embeds;
                if (m.Attachments.IsSpecified) m.Attachments = m.Attachments;
            });

            await FollowupAsync("Link component added successfully!", ephemeral: true);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error adding link component to message {MessageId}", messageId);
            
            await FollowupAsync("An error occurred while adding the link component. Please try again.", ephemeral: true);
        }
    }

    public class AddLinkComponentModal : IModal
    {
        [InputLabel("URL")]
        [ModalTextInput("url")]
        public string Url { get; set; } = "";

        [InputLabel("Label")]
        [ModalTextInput("label")]
        public string Label { get; set; } = "";

        [InputLabel("Emoji ID (Optional)")]
        [ModalTextInput("emojiId")]
        public string EmojiId { get; set; } = "";

        public string Title => "Add Link Component";
    }
}