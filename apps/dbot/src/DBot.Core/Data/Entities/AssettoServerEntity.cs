using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DBot.Core.Data.Entities;

/// <summary>
///     Database entity representing an Assetto Corsa server
/// </summary>
[Index(nameof(ApiUrl), IsUnique = true)]
public class AssettoServerEntity : BaseEntity
{
    [Required] [Url] public required string ApiUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastChecked { get; set; }

    // Navigation properties
    public virtual ICollection<AssettoServerMonitorEntity> StatusMessages { get; set; } =
        new List<AssettoServerMonitorEntity>();

    public virtual ICollection<AssettoServerGuildEntity> GuildConfigurations { get; set; } =
        new List<AssettoServerGuildEntity>();
}