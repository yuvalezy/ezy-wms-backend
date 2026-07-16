using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition> {
    public void Configure(EntityTypeBuilder<ReportDefinition> builder) {
        // Slug is the URL identity (/reports/{slug}, POST /api/report/{slug}/run) so it must be unique —
        // but only among live rows. ReportDefinition is a BaseEntity, so a row can be soft-deleted
        // (Deleted = 1); an unfiltered unique index would keep a deleted report's slug reserved forever.
        builder.HasIndex(e => e.Slug)
            .IsUnique()
            .HasFilter("[Deleted] = 0")
            .HasDatabaseName("IX_ReportDefinitions_Slug");

        // Author SQL is arbitrarily long; the default nvarchar(450)-ish inference would truncate a real query.
        builder.Property(e => e.Sql)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.CountSql)
            .HasColumnType("nvarchar(max)");

        // Columns and variables are owned by the definition: they only ever exist as part of it, they are
        // replaced wholesale on update, and they must not outlive it.
        builder.HasMany(e => e.Columns)
            .WithOne(c => c.ReportDefinition)
            .HasForeignKey(c => c.ReportDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Variables)
            .WithOne(v => v.ReportDefinition)
            .HasForeignKey(v => v.ReportDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReportColumnConfiguration : IEntityTypeConfiguration<ReportColumn> {
    public void Configure(EntityTypeBuilder<ReportColumn> builder) {
        builder.HasKey(e => e.Id);

        // Key is both the row-dictionary key of every returned row and the ORDER BY whitelist entry, so a
        // duplicate would make both ambiguous. Enforced here as well as in ReportService because it is an
        // invariant the execution engine relies on, not merely an input-validation nicety.
        builder.HasIndex(e => new { e.ReportDefinitionId, e.Key })
            .IsUnique()
            .HasDatabaseName("IX_ReportColumns_ReportDefinitionId_Key");
    }
}

public class ReportVariableConfiguration : IEntityTypeConfiguration<ReportVariable> {
    public void Configure(EntityTypeBuilder<ReportVariable> builder) {
        builder.HasKey(e => e.Id);

        // SqlCommand.Parameters lookup is case-insensitive, so @foo and @Foo are one parameter — a
        // "duplicate" that only differs in case is still a duplicate. SQL Server's default collation makes
        // this index case-insensitive too, which is exactly the comparison we want.
        builder.HasIndex(e => new { e.ReportDefinitionId, e.Name })
            .IsUnique()
            .HasDatabaseName("IX_ReportVariables_ReportDefinitionId_Name");

        builder.Property(e => e.StaticOptions)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.LookupSql)
            .HasColumnType("nvarchar(max)");
    }
}

public class AuthorizationGroupReportConfiguration : IEntityTypeConfiguration<AuthorizationGroupReport> {
    public void Configure(EntityTypeBuilder<AuthorizationGroupReport> builder) {
        builder.HasKey(e => new { e.AuthorizationGroupId, e.ReportDefinitionId });

        builder.HasOne(e => e.AuthorizationGroup)
            .WithMany(g => g.Reports)
            .HasForeignKey(e => e.AuthorizationGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // ReportDefinition has no inverse collection on purpose — grants are read from the group side, and
        // the report aggregate has no business carrying them around.
        builder.HasOne(e => e.ReportDefinition)
            .WithMany()
            .HasForeignKey(e => e.ReportDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // The composite PK already covers AuthorizationGroupId-leading lookups (the access check); this
        // covers the other direction — "which groups is this report granted to".
        builder.HasIndex(e => e.ReportDefinitionId)
            .HasDatabaseName("IX_AuthorizationGroupReports_ReportDefinitionId");
    }
}

public class ReportExecutionAuditConfiguration : IEntityTypeConfiguration<ReportExecutionAudit> {
    public void Configure(EntityTypeBuilder<ReportExecutionAudit> builder) {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ParametersJson)
            .HasColumnType("nvarchar(max)");

        // Deliberately no FK to ReportDefinitions or Users. This is append-only forensic data that has to
        // outlive what it points at: a cascade would erase the trail with the report, and a restrict would
        // block deleting any report that had ever been run.
        builder.HasIndex(e => new { e.ReportDefinitionId, e.StartedAtUtc })
            .HasDatabaseName("IX_ReportExecutionAudits_ReportDefinition_StartedAt");

        builder.HasIndex(e => e.StartedAtUtc)
            .HasDatabaseName("IX_ReportExecutionAudits_StartedAt");
    }
}
