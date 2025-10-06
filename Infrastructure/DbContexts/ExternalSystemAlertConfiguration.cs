using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class ExternalSystemAlertConfiguration : IEntityTypeConfiguration<ExternalSystemAlert> {
    public void Configure(EntityTypeBuilder<ExternalSystemAlert> builder) {
        // Create unique index on ObjectType and ExternalUserId
        builder.HasIndex(e => new { e.ObjectType, e.ExternalUserId })
            .IsUnique()
            .HasDatabaseName("IX_ExternalSystemAlerts_ObjectType_ExternalUserId");
    }
}
