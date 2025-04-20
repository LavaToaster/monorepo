using Discord;
using Discord.Interactions;

namespace DBot.Bot.InteractionModules;

public class MessageInteractionsModule : InteractionModuleBase<SocketInteractionContext>
{
    [MessageCommand("Edit Message")]
    public async Task EditMessage(IMessage message)
    {
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
}