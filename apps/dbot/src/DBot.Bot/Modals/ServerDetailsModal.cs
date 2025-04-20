using Discord;
using Discord.Interactions;

namespace DBot.Bot.Modals;

public class ServerDetailsModal : IModal
{
    [InputLabel("Description")]
    [ModalTextInput("description", TextInputStyle.Paragraph, maxLength: 1000)]
    public string Description { get; set; } = "";

    [InputLabel("Thumbnail URL")]
    [ModalTextInput("thumbnail", maxLength: 500)]
    public string ThumbnailUrl { get; set; } = "";

    public string Title => "Server Status Details";
}