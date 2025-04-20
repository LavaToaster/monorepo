using DBot.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DBot.Core.Data.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AssettoServerEntity> AssettoServers { get; set; }
    public DbSet<AssettoServerMonitorEntity> StatusMessages { get; set; }
    public DbSet<AssettoServerGuildEntity> GuildConfigurations { get; set; }
    public DbSet<RoleMirrorCandidateEntity> RoleMirrorCandidates { get; set; }
    
    public DbSet<RoleMirrorMappingEntity> RoleMirrorMappings { get; set; }
}