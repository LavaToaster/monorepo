using DBot.Core.Data.Entities;

namespace DBot.Bot.Models.Assetto;

/// <summary>
///     Result of adding a new Assetto server
/// </summary>
public class AddServerResult
{
    public required AssettoServerEntity Server { get; init; }
    public required AssettoServerGuildEntity GuildConfiguration { get; init; }
    public bool IsNewServer { get; init; }
}