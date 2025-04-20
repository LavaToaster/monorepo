using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DBot.Core.Data.Entities;

/// <summary>
///     Represents an available role for mirroring.
/// </summary>
[Index(nameof(GuildId), nameof(RoleId), IsUnique = true)]
public class RoleMirrorCandidateEntity : BaseEntity
{
    [Required] public required ulong GuildId { get; set; }
    [Required] public required ulong RoleId { get; set; }
    
    [InverseProperty(nameof(RoleMirrorMappingEntity.SourceRole))]
    public virtual ICollection<RoleMirrorMappingEntity> SourceRoleConfigurations { get; set; } = new List<RoleMirrorMappingEntity>();
    [InverseProperty(nameof(RoleMirrorMappingEntity.TargetRole))]
    public virtual ICollection<RoleMirrorMappingEntity> TargetRoleConfigurations { get; set; } = new List<RoleMirrorMappingEntity>();
}