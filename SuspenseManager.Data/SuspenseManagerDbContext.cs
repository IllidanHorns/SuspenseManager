using Microsoft.EntityFrameworkCore;
using Models;

namespace Data;

public class SuspenseManagerDbContext : DbContext
{
    public SuspenseManagerDbContext(DbContextOptions<SuspenseManagerDbContext> options)
        : base(options)
    {
    }

    public DbSet<SuspenseLine> SuspenseLines { get; set; }
    public DbSet<SuspenseGroup> SuspenseGroups { get; set; }
    public DbSet<SuspenseGroupLink> SuspenseGroupLinks { get; set; }
    public DbSet<GroupMetadata> GroupMetadata { get; set; }
    public DbSet<GroupMetaRights> GroupMetaRights { get; set; }
    public DbSet<CatalogProduct> CatalogProducts { get; set; }
    public DbSet<CatalogProductType> CatalogProductTypes { get; set; }
    public DbSet<CatalogProductRights> CatalogProductRights { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Territory> Territories { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Rights> Rights { get; set; }
    public DbSet<AccountRightsLink> AccountRightsLinks { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<StatusDictionary> StatusDictionary { get; set; }
    public DbSet<SuspenseGroupLog> SuspenseGroupLogs { get; set; }
    public DbSet<SuspenseLineLog> SuspenseLineLogs { get; set; }
    public DbSet<BackOfficeTask> BackOfficeTasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SuspenseManagerDbContext).Assembly);
    }
}
