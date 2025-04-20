using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DBot.Core.Data.Entities;

/// <summary>
///     Represents the configuration for an Assetto server within a specific Discord guild
/// </summary>
[Index(nameof(GuildId), nameof(AssettoServerEntityId), IsUnique = true)]
public class AssettoServerGuildEntity : BaseEntity
{
    [Required] public required ulong GuildId { get; set; }

    [Required] public Guid AssettoServerEntityId { get; set; }

    [Required] [MaxLength(200)] public required string DisplayName { get; set; }

    [ForeignKey(nameof(AssettoServerEntityId))]
    public virtual AssettoServerEntity ServerEntity { get; set; } = null!;
}