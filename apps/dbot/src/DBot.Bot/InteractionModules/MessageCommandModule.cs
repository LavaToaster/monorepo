using System.ComponentModel;
using Discord;
using Discord.Interactions;

namespace DBot.Bot.InteractionModules;

[DefaultMemberPermissions(GuildPermission.Administrator)]
[Group("message", "Message commands")]
public class MessageCommandModule(ILogger<MessageCommandModule> logger)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("echo", "Echo a message")]
    public async Task EchoMessageAsync([Description("Message to echo")] string message)
    {
        await DeferAsync(true);

        await Context.Channel.SendMessageAsync(message);

        await FollowupAsync("Message sent!", ephemeral: true);
    }

    [SlashCommand("thread", "Create a thread with a message")]
    public async Task CreateThreadAsync(
        [Description("Forum channel to create thread in")]
        IForumChannel? forumChannel,
        [Description("Title")] string title,
        [Description("Content")] string content)
    {
        await DeferAsync(true);

        if (forumChannel is null)
        {
            await FollowupAsync("Forum channel is required.", ephemeral: true);
            return;
        }

        try
        {
            var thread = await forumChannel.CreatePostAsync(title, text: content);

            await FollowupAsync($"Thread created: {thread.Mention}", ephemeral: true);
        } catch (Exception ex) {
            logger.LogError(ex, "Failed to create thread");
            await FollowupAsync("Failed to create thread.", ephemeral: true);
        }
    }
}