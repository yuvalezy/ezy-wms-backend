using Core.Entities;
using Core.Enums;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ApprovalWorkflowService(SystemDbContext db) : IApprovalWorkflowService {
    public async Task<ApprovalWorkflow> CreateApprovalRequestAsync(
        Guid objectId,
        ApprovalObjectType objectType,
        SessionInfo sessionInfo) {
        // Check if there's already a pending approval for this object
        var existing = await db.ApprovalWorkflows
            .FirstOrDefaultAsync(aw =>
                aw.ObjectId == objectId &&
                aw.ObjectType == objectType &&
                aw.ApprovalStatus == ApprovalStatus.Pending);

        if (existing != null) {
            return existing;
        }

        var workflow = new ApprovalWorkflow {
            ObjectId          = objectId,
            ObjectType        = objectType,
            RequestedByUserId = sessionInfo.Guid,
            RequestedAt       = DateTime.UtcNow,
            ApprovalStatus    = ApprovalStatus.Pending,
            CreatedAt         = DateTime.UtcNow,
            CreatedByUserId   = sessionInfo.Guid
        };

        db.ApprovalWorkflows.Add(workflow);
        await db.SaveChangesAsync();

        return workflow;
    }

    public async Task<ApprovalWorkflow> ApproveAsync(
        Guid objectId,
        ApprovalObjectType objectType,
        SessionInfo sessionInfo,
        string? comments = null) {
        var workflow = await GetPendingWorkflowAsync(objectId, objectType);
        if (workflow == null) {
            throw new InvalidOperationException($"No pending approval workflow found for {objectType} with ID {objectId}");
        }

        workflow.ApprovalStatus    = ApprovalStatus.Approved;
        workflow.ReviewedByUserId  = sessionInfo.Guid;
        workflow.ReviewedAt        = DateTime.UtcNow;
        workflow.ReviewComments    = comments;
        workflow.UpdatedAt         = DateTime.UtcNow;
        workflow.UpdatedByUserId   = sessionInfo.Guid;

        await db.SaveChangesAsync();
        return workflow;
    }

    public async Task<ApprovalWorkflow> RejectAsync(
        Guid objectId,
        ApprovalObjectType objectType,
        string rejectionReason,
        SessionInfo sessionInfo,
        string? comments = null) {
        var workflow = await GetPendingWorkflowAsync(objectId, objectType);
        if (workflow == null) {
            throw new InvalidOperationException($"No pending approval workflow found for {objectType} with ID {objectId}");
        }

        workflow.ApprovalStatus    = ApprovalStatus.Rejected;
        workflow.ReviewedByUserId  = sessionInfo.Guid;
        workflow.ReviewedAt        = DateTime.UtcNow;
        workflow.RejectionReason   = rejectionReason;
        workflow.ReviewComments    = comments;
        workflow.UpdatedAt         = DateTime.UtcNow;
        workflow.UpdatedByUserId   = sessionInfo.Guid;

        await db.SaveChangesAsync();
        return workflow;
    }

    public async Task<ApprovalWorkflow?> CancelAsync(
        Guid objectId,
        ApprovalObjectType objectType) {
        var workflow = await GetPendingWorkflowAsync(objectId, objectType);
        if (workflow == null) {
            return null;
        }

        workflow.ApprovalStatus  = ApprovalStatus.Cancelled;
        workflow.UpdatedAt       = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return workflow;
    }

    public async Task<ApprovalWorkflow?> GetApprovalWorkflowAsync(
        Guid objectId,
        ApprovalObjectType objectType) {
        return await db.ApprovalWorkflows
            .Include(aw => aw.RequestedByUser)
            .Include(aw => aw.ReviewedByUser)
            .FirstOrDefaultAsync(aw =>
                aw.ObjectId == objectId &&
                aw.ObjectType == objectType);
    }

    public async Task<bool> HasPendingApprovalAsync(
        Guid objectId,
        ApprovalObjectType objectType) {
        return await db.ApprovalWorkflows
            .AnyAsync(aw =>
                aw.ObjectId == objectId &&
                aw.ObjectType == objectType &&
                aw.ApprovalStatus == ApprovalStatus.Pending);
    }

    private async Task<ApprovalWorkflow?> GetPendingWorkflowAsync(
        Guid objectId,
        ApprovalObjectType objectType) {
        return await db.ApprovalWorkflows
            .Include(aw => aw.RequestedByUser)
            .FirstOrDefaultAsync(aw =>
                aw.ObjectId == objectId &&
                aw.ObjectType == objectType &&
                aw.ApprovalStatus == ApprovalStatus.Pending);
    }
}
