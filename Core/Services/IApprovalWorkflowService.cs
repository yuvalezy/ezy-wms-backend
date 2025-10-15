using Core.Entities;
using Core.Enums;
using Core.Models;

namespace Core.Services;

public interface IApprovalWorkflowService {
    /// <summary>
    /// Creates a new approval workflow request
    /// </summary>
    Task<ApprovalWorkflow> CreateApprovalRequestAsync(
        Guid objectId,
        ApprovalObjectType objectType,
        SessionInfo sessionInfo);

    /// <summary>
    /// Approves an approval workflow
    /// </summary>
    Task<ApprovalWorkflow> ApproveAsync(
        Guid objectId,
        ApprovalObjectType objectType,
        SessionInfo sessionInfo,
        string? comments = null);

    /// <summary>
    /// Rejects an approval workflow
    /// </summary>
    Task<ApprovalWorkflow> RejectAsync(
        Guid objectId,
        ApprovalObjectType objectType,
        string rejectionReason,
        SessionInfo sessionInfo,
        string? comments = null);

    /// <summary>
    /// Cancels a pending approval workflow
    /// </summary>
    Task<ApprovalWorkflow?> CancelAsync(
        Guid objectId,
        ApprovalObjectType objectType);

    /// <summary>
    /// Gets the approval workflow for a specific object
    /// </summary>
    Task<ApprovalWorkflow?> GetApprovalWorkflowAsync(
        Guid objectId,
        ApprovalObjectType objectType);

    /// <summary>
    /// Checks if an object has a pending approval
    /// </summary>
    Task<bool> HasPendingApprovalAsync(
        Guid objectId,
        ApprovalObjectType objectType);
}
