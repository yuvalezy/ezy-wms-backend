using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class WmsAlertConfiguration : IEntityTypeConfiguration<WmsAlert> {
    public void Configure(EntityTypeBuilder<WmsAlert> builder) {
        builder.HasIndex(e => new { e.UserId, e.IsRead });
        builder.HasIndex(e => e.ObjectId);
        builder.HasIndex(e => e.CreatedAt);
    }
}
