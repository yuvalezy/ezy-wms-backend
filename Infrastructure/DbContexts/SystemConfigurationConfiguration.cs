using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class SystemConfigurationEntryConfiguration : IEntityTypeConfiguration<SystemConfigurationEntry> {
    public void Configure(EntityTypeBuilder<SystemConfigurationEntry> builder) {
        builder.ToTable("SystemConfiguration");

        builder.HasKey(e => e.Section);

        builder.Property(e => e.Section)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Json)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Version)
            .HasDefaultValue(1);

        builder.Property(e => e.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.RowVersion)
            .IsRowVersion();
    }
}

public class SystemConfigurationAuditConfiguration : IEntityTypeConfiguration<SystemConfigurationAudit> {
    public void Configure(EntityTypeBuilder<SystemConfigurationAudit> builder) {
        builder.ToTable("SystemConfigurationAudit");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Section)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Json)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.ChangeType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(e => e.ChangedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.Note)
            .HasMaxLength(400);

        builder.HasIndex(e => new { e.Section, e.ChangedAtUtc })
            .HasDatabaseName("IX_SystemConfigurationAudit_Section_ChangedAt");
    }
}

public class ConfigurationMigrationStateConfiguration : IEntityTypeConfiguration<ConfigurationMigrationStateEntity> {
    public void Configure(EntityTypeBuilder<ConfigurationMigrationStateEntity> builder) {
        builder.ToTable("ConfigurationMigrationState");

        builder.HasKey(e => e.Id);

        // Single fixed-id row; never auto-generated.
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(e => e.Source)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(e => e.ArchivePath)
            .HasMaxLength(400);

        builder.Property(e => e.Detail)
            .HasColumnType("nvarchar(max)");
    }
}
