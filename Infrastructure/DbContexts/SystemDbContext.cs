using Core.Entities;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class SystemDbContext : DbContext {
    public SystemDbContext(DbContextOptions<SystemDbContext> options) : base(options) {
        ChangeTracker.LazyLoadingEnabled = false;
    }

    public DbSet<AuthorizationGroup> AuthorizationGroups { get; set; }
    public DbSet<User>               Users               { get; set; }
    
    // Objects Entities
    public DbSet<GoodsReceipt>       GoodsReceipts       { get; set; }
    public DbSet<InventoryCounting>  InventoryCountings  { get; set; }
    public DbSet<Transfer>           Transfers           { get; set; }
    public DbSet<PickList>           PickLists           { get; set; }
    
    // Object Lines Entites
    public DbSet<GoodsReceiptLine>      GoodsReceiptLines      { get; set; }
    public DbSet<GoodsReceiptTarget>    GoodsReceiptTargets    { get; set; }
    public DbSet<GoodsReceiptDocument>  GoodsReceiptDocuments  { get; set; }
    public DbSet<GoodsReceiptSource>    GoodsReceiptSources    { get; set; }
    public DbSet<InventoryCountingLine> InventoryCountingLines { get; set; }
    public DbSet<TransferLine>          TransferLines          { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AuthorizationGroupConfiguration());
        
        
        // Apply database-specific configurations
        if (Database.IsSqlServer()) {
            SqlConfigurations.ConfigureForSqlServer(modelBuilder);
        }
        // Future: Add SAP HANA configuration
        // else if (Database.IsHana()) {
        //     ConfigureForHana(modelBuilder);
        // }
    }
}

public class UserConfiguration: IEntityTypeConfiguration<User> {
    public void Configure(EntityTypeBuilder<User> builder) {
        builder
            .HasOne<AuthorizationGroup>(v => v.AuthorizationGroup)
            .WithMany()
            .HasForeignKey(a => a.AuthorizationGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuthorizationGroupConfiguration: IEntityTypeConfiguration<AuthorizationGroup> {
    public void Configure(EntityTypeBuilder<AuthorizationGroup> builder) {
        // Store the Authorization enum collection as a JSON string
        builder.Property(e => e.Authorizations)
            .HasConversion(
                v => string.Join(',', v.Select(a => (int)a)),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => (RoleType)int.Parse(s))
                     .ToList()
            );
    }
}