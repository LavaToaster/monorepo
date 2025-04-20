namespace DBot.Core.Data.Entities;

/// <summary>
/// Defines how roles should be synchronized between source and target.
/// </summary>
public enum RoleSync
{
    /// <summary>
    /// Strict sync ensures that target roles exactly match source roles, removing any roles not in the mapping.
    /// </summary>
    Strict,
    
    /// <summary>
    /// Preserve sync adds mapped roles but doesn't remove other roles from the target.
    /// </summary>
    Preserve
}
