using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

public abstract class BaseEntity {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    // Timestamps
    [Required]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdateDate { get; set; }
    public bool      Deleted    { get; set; }

    public DateTime? DeletedAt { get; set; }
}