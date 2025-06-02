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

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AuthorizationGroupConfiguration());
        
        // Apply database-specific configurations
        if (Database.IsSqlServer()) {
            ConfigureForSqlServer(modelBuilder);
        }
        // Future: Add SAP HANA configuration
        // else if (Database.IsHana()) {
        //     ConfigureForHana(modelBuilder);
        // }
    }
    
    private void ConfigureForSqlServer(ModelBuilder modelBuilder) {
        // Configure string columns for SQL Server
        modelBuilder.Entity<User>(entity => {
            entity.Property(e => e.FullName).HasColumnType("nvarchar(50)");
            entity.Property(e => e.Password).HasColumnType("nvarchar(255)");
            entity.Property(e => e.Email).HasColumnType("nvarchar(100)");
            entity.Property(e => e.Position).HasColumnType("nvarchar(100)");
            entity.HasQueryFilter(e => !e.Deleted);
        });
        
        modelBuilder.Entity<AuthorizationGroup>(entity => {
            entity.Property(e => e.Name).HasColumnType("nvarchar(50)");
            entity.Property(e => e.Description).HasColumnType("nvarchar(200)");
            entity.HasQueryFilter(e => !e.Deleted);
        });
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