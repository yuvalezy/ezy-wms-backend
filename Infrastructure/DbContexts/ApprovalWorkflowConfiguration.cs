using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class ApprovalWorkflowConfiguration : IEntityTypeConfiguration<ApprovalWorkflow> {
    public void Configure(EntityTypeBuilder<ApprovalWorkflow> builder) {
        builder.ToTable("ApprovalWorkflows");

        builder.HasKey(aw => aw.Id);

        // Indexes for performance
        builder.HasIndex(aw => new { aw.ObjectId, aw.ObjectType })
            .HasDatabaseName("IX_ApprovalWorkflows_ObjectId_ObjectType");

        builder.HasIndex(aw => aw.ApprovalStatus)
            .HasDatabaseName("IX_ApprovalWorkflows_ApprovalStatus");

        builder.HasIndex(aw => aw.RequestedByUserId)
            .HasDatabaseName("IX_ApprovalWorkflows_RequestedByUserId");

        builder.HasIndex(aw => aw.ReviewedByUserId)
            .HasDatabaseName("IX_ApprovalWorkflows_ReviewedByUserId");

        // Configure relationships
        builder.HasOne(aw => aw.RequestedByUser)
            .WithMany()
            .HasForeignKey(aw => aw.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(aw => aw.ReviewedByUser)
            .WithMany()
            .HasForeignKey(aw => aw.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
