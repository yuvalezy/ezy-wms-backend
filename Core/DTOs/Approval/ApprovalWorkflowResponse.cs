using Core.Entities;
using Core.Enums;

namespace Core.DTOs.Approval;

public class ApprovalWorkflowResponse {
    public Guid                 Id                  { get; set; }
    public Guid                 ObjectId            { get; set; }
    public ApprovalObjectType   ObjectType          { get; set; }
    public Guid                 RequestedByUserId   { get; set; }
    public User?                RequestedByUser     { get; set; }
    public DateTime             RequestedAt         { get; set; }
    public ApprovalStatus       ApprovalStatus      { get; set; }
    public Guid?                ReviewedByUserId    { get; set; }
    public User?                ReviewedByUser      { get; set; }
    public DateTime?            ReviewedAt          { get; set; }
    public string?              RejectionReason     { get; set; }
    public string?              ReviewComments      { get; set; }

    public static ApprovalWorkflowResponse FromApprovalWorkflow(ApprovalWorkflow workflow) {
        return new ApprovalWorkflowResponse {
            Id                = workflow.Id,
            ObjectId          = workflow.ObjectId,
            ObjectType        = workflow.ObjectType,
            RequestedByUserId = workflow.RequestedByUserId,
            RequestedByUser   = workflow.RequestedByUser,
            RequestedAt       = workflow.RequestedAt,
            ApprovalStatus    = workflow.ApprovalStatus,
            ReviewedByUserId  = workflow.ReviewedByUserId,
            ReviewedByUser    = workflow.ReviewedByUser,
            ReviewedAt        = workflow.ReviewedAt,
            RejectionReason   = workflow.RejectionReason,
            ReviewComments    = workflow.ReviewComments
        };
    }
}
