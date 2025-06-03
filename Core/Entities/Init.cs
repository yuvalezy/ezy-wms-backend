using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

// GRPO Main table
[Table("@LW_YUVAL08_GRPO")]
public class GRPO : BaseEntity {
    [StringLength(15)]
    public string? U_CardCode { get; set; }

    public DateTime? U_Date { get; set; }

    public int? U_empID { get; set; }

    [StringLength(1)]
    [Column(TypeName = "char(1)")]
    public string U_Status { get; set; } = "O";

    public DateTime? U_StatusDate { get; set; }

    public int? U_StatusEmpID { get; set; }

    [Required]
    [StringLength(1)]
    [Column(TypeName = "char(1)")]
    public string U_Type { get; set; } = "A";

    [StringLength(8)]
    public string? U_WhsCode { get; set; }

    // Navigation properties
    public virtual ICollection<GRPOLine>     Lines     { get; set; } = new List<GRPOLine>();
    public virtual ICollection<GRPOTarget>   Targets   { get; set; } = new List<GRPOTarget>();
    public virtual ICollection<GRPODocument> Documents { get; set; } = new List<GRPODocument>();
    public virtual ICollection<GRPOSource>   Sources   { get; set; } = new List<GRPOSource>();
}

// GRPO1 Lines table
[Table("@LW_YUVAL08_GRPO1")]
public class GRPOLine : BaseEntity {
    [Required]
    [StringLength(254)]
    public string U_BarCode { get; set; } = string.Empty;

    [Column(TypeName = "ntext")]
    public string? U_Comments { get; set; }

    [Required]
    public DateTime U_Date { get; set; }

    [Required]
    public int U_empID { get; set; }

    [Required]
    [StringLength(50)]
    public string U_ItemCode { get; set; } = string.Empty;

    [Required]
    public int U_LineID { get; set; }

    [Required]
    [StringLength(1)]
    [Column(TypeName = "char(1)")]
    public string U_LineStatus { get; set; } = "O";

    public DateTime? U_QtyTimeStamp { get; set; }

    public int? U_QtyUserSign { get; set; }

    [Required]
    [Column(TypeName = "decimal(16,6)")]
    public decimal U_Quantity { get; set; }

    public int? U_StatusReason { get; set; }

    public DateTime? U_StatusTimeStamp { get; set; }

    public int? U_StatusUserSign { get; set; }

    [Required]
    public int U_Unit { get; set; } = 0;

    [Required]
    [StringLength(254)]
    public string U_ID { get; set; } = string.Empty;

    // Navigation property
    [ForeignKey("U_ID")]
    public virtual GRPO GRPO { get; set; } = null!;
}

// GRPO2 Target table
[Table("@LW_YUVAL08_GRPO2")]
public class GRPOTarget : BaseEntity {
    [Required]
    [StringLength(50)]
    public string U_ItemCode { get; set; } = string.Empty;

    [Required]
    public int U_LineID { get; set; }

    [Required]
    public int U_TargetEntry { get; set; }

    [Required]
    public int U_TargetLine { get; set; }

    [Required]
    [Column(TypeName = "decimal(16,6)")]
    public decimal U_TargetQty { get; set; }

    [Required]
    [StringLength(1)]
    [Column(TypeName = "char(1)")]
    public string U_TargetStatus { get; set; } = "O";

    [Required]
    public int U_TargetType { get; set; }

    [Required]
    [StringLength(254)]
    public string U_ID { get; set; } = string.Empty;

    // Navigation property
    [ForeignKey("U_ID")]
    public virtual GRPO GRPO { get; set; } = null!;
}

// GRPO3 Document table
[Table("@LW_YUVAL08_GRPO3")]
public class GRPODocument : BaseEntity {
    [Required]
    public int U_DocEntry { get; set; }

    [Required]
    public int U_LineID { get; set; }

    [Required]
    public int U_ObjType { get; set; }

    [Required]
    [StringLength(254)]
    public string U_ID { get; set; } = string.Empty;

    // Navigation property
    [ForeignKey("U_ID")]
    public virtual GRPO GRPO { get; set; } = null!;
}

// GRPO4 Source table
[Table("@LW_YUVAL08_GRPO4")]
public class GRPOSource : BaseEntity {
    [Required]
    public int U_LineID { get; set; }

    [Required]
    [Column(TypeName = "decimal(16,6)")]
    public decimal U_Quantity { get; set; }

    [Required]
    public int U_SourceEntry { get; set; }

    [Required]
    public int U_SourceLine { get; set; }

    [Required]
    public int U_SourceType { get; set; }

    [Required]
    [StringLength(254)]
    public string U_ID { get; set; } = string.Empty;

    // Navigation property
    [ForeignKey("U_ID")]
    public virtual GRPO GRPO { get; set; } = null!;
}

