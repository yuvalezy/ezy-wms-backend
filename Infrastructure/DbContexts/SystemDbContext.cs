using System.Linq.Expressions;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DbContexts;

public class SystemDbContext : DbContext {
    public SystemDbContext(DbContextOptions<SystemDbContext> options) : base(options) {
        ChangeTracker.LazyLoadingEnabled = false;
    }

    public DbSet<AuthorizationGroup> AuthorizationGroups { get; set; }
    public DbSet<User>               Users               { get; set; }
    public DbSet<CancellationReason> CancellationReasons { get; set; }

    // Objects Entities
    public DbSet<GoodsReceipt>      GoodsReceipts      { get; set; }
    public DbSet<InventoryCounting> InventoryCountings { get; set; }
    public DbSet<Transfer>          Transfers          { get; set; }
    public DbSet<PickList>          PickLists          { get; set; }

    // Object Lines Entites
    public DbSet<GoodsReceiptLine>      GoodsReceiptLines      { get; set; }
    public DbSet<GoodsReceiptTarget>    GoodsReceiptTargets    { get; set; }
    public DbSet<GoodsReceiptDocument>  GoodsReceiptDocuments  { get; set; }
    public DbSet<GoodsReceiptSource>    GoodsReceiptSources    { get; set; }
    public DbSet<InventoryCountingLine> InventoryCountingLines { get; set; }
    public DbSet<TransferLine>          TransferLines          { get; set; }

    // Package Entities
    public DbSet<Package>                Packages                { get; set; }
    public DbSet<PackageContent>         PackageContents         { get; set; }
    public DbSet<PackageTransaction>     PackageTransactions     { get; set; }
    public DbSet<PackageLocationHistory> PackageLocationHistory  { get; set; }
    public DbSet<PackageInconsistency>   PackageInconsistencies  { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AuthorizationGroupConfiguration());
        modelBuilder.ApplyConfiguration(new CancellationReasonConfiguration());
        modelBuilder.ApplyConfiguration(new GoodsReceiptConfiguration());
        modelBuilder.ApplyConfiguration(new GoodsReceiptLineConfiguration());
        modelBuilder.ApplyConfiguration(new GoodsReceiptTargetConfiguration());
        modelBuilder.ApplyConfiguration(new GoodsReceiptDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new GoodsReceiptSourceConfiguration());
        modelBuilder.ApplyConfiguration(new TransferConfiguration());
        modelBuilder.ApplyConfiguration(new TransferLineConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryCountingConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryCountingLineConfiguration());
        modelBuilder.ApplyConfiguration(new PickListConfiguration());
        
        // Package configurations
        modelBuilder.ApplyConfiguration(new PackageConfiguration());
        modelBuilder.ApplyConfiguration(new PackageContentConfiguration());
        modelBuilder.ApplyConfiguration(new PackageTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new PackageLocationHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new PackageInconsistencyConfiguration());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()) {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;
            var entityBuilder = modelBuilder.Entity(entityType.ClrType);


            if (Database.IsSqlServer()) {
                entityBuilder
                    .Property(nameof(BaseEntity.CreatedAt))
                    .HasDefaultValueSql("GETUTCDATE()");
            }

            // Create query filter using reflection
            var parameter  = Expression.Parameter(entityType.ClrType, "e");
            var property   = Expression.Property(parameter, nameof(BaseEntity.Deleted));
            var comparison = Expression.Equal(property, Expression.Constant(false));
            var lambda     = Expression.Lambda(comparison, parameter);

            // Configure audit relationships if properties exist
            ConfigureAuditRelationships(entityBuilder, entityType.ClrType);

            entityBuilder.HasQueryFilter(lambda);
        }


        // Future: Add SAP HANA configuration
        // else if (Database.IsHana()) {
        //     ConfigureForHana(modelBuilder);
        // }
    }

    private void ConfigureAuditRelationships(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder entityBuilder, Type entityType) {
        if (entityType == typeof(User))
            return;

        // Check if entity has audit properties
        var createdByUserProp = entityType.GetProperty("CreatedByUser");
        var updatedByUserProp = entityType.GetProperty("UpdatedByUser");

        if (createdByUserProp != null) {
            entityBuilder.HasOne("CreatedByUser")
                .WithMany()
                .HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict);
        }

        if (updatedByUserProp != null) {
            entityBuilder.HasOne("UpdatedByUser")
                .WithMany()
                .HasForeignKey("UpdatedByUserId")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}