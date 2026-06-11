using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class WmsSessionConfiguration : IEntityTypeConfiguration<WmsSession> {
    public void Configure(EntityTypeBuilder<WmsSession> builder) {
        builder.ToTable("WmsSessions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.SessionData)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_WmsSessions_ExpiresAt");
    }
}
