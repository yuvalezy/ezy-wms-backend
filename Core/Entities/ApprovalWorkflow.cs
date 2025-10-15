using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

[Table("ApprovalWorkflows")]
public class ApprovalWorkflow : BaseEntity {
    // What is being approved
    [Required]
    public Guid ObjectId { get; set; }

    [Required]
    public ApprovalObjectType ObjectType { get; set; }

    // Approval flow tracking
    [Required]
    [ForeignKey("RequestedByUser")]
    public Guid RequestedByUserId { get; set; }

    [Required]
    public DateTime RequestedAt { get; set; }

    [Required]
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

    // Supervisor action tracking
    [ForeignKey("ReviewedByUser")]
    public Guid? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(1000)]
    public string? RejectionReason { get; set; }

    [MaxLength(2000)]
    public string? ReviewComments { get; set; }

    // Navigation properties
    public User RequestedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
