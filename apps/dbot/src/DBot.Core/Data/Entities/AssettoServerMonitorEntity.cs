using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DBot.Core.Data.Entities;

/// <summary>
///     Represents a Discord message tracking an Assetto Corsa server's status
/// </summary>
[Index(nameof(GuildId), nameof(ChannelId), nameof(MessageId), IsUnique = true)]
public class AssettoServerMonitorEntity : BaseEntity
{
    [Required] public required ulong GuildId { get; set; }

    [Required] public required ulong ChannelId { get; set; }

    [Required] public required ulong MessageId { get; set; }

    [Required] [MaxLength(1024)] public required string ThumbnailUrl { get; set; }

    [Required] [MaxLength(2000)] public required string Description { get; set; }

    // [Required]
    // [MaxLength(200)]
    // public required string Emoji { get; set; }

    [Required] public Guid ServerEntityId { get; set; }

    [ForeignKey(nameof(ServerEntityId))] public virtual AssettoServerEntity ServerEntity { get; set; } = null!;
}