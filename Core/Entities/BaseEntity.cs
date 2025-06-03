using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

public abstract class BaseEntity {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    // Timestamps
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public DateTime? UpdatedAt { get; set; }
    
    public Guid? UpdatedByUserId { get; set; }

    public User? UpdatedByUser { get; set; }

    public bool      Deleted   { get; set; }

    public DateTime? DeletedAt { get; set; }
}