using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DBot.Core.Data.Entities;

/// <summary>
///     Represents the configuration for role mirroring.
/// </summary>
[Index(nameof(SourceRoleId), nameof(TargetRoleId), IsUnique = true)]
public class RoleMirrorMappingEntity : BaseEntity
{
    [Required] public required Guid SourceRoleId { get; set; }
    [Required] public required Guid TargetRoleId { get; set; }

    [ForeignKey(nameof(SourceRoleId))] public virtual RoleMirrorCandidateEntity SourceRole { get; set; } = null!;
    [ForeignKey(nameof(TargetRoleId))] public virtual RoleMirrorCandidateEntity TargetRole { get; set; } = null!;
}