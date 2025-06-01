using Core.Entities;
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