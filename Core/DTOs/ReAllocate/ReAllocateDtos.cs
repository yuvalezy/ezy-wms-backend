using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.ReAllocate;

/// <summary>One bin currently holding the item, with the quantity that may be moved out of it.</summary>
public class ReAllocateSourceBin {
    public int     BinEntry  { get; set; }
    public string  BinCode   { get; set; } = string.Empty;
    /// <summary>SAP bin on-hand (OIBQ), base units.</summary>
    public decimal OnHand    { get; set; }
    /// <summary>Held in WMS (picked-not-synced pick lists, open transfer lines), base units.</summary>
    public decimal Reserved  { get; set; }
    /// <summary>OnHand − Reserved, never negative.</summary>
    public decimal Available { get; set; }
}

public class ReAllocateSourcesResponse {
    public string                    ItemCode       { get; set; } = string.Empty;
    public List<ReAllocateSourceBin> Bins           { get; set; } = [];
    public decimal                   TotalAvailable { get; set; }
}

public class ReAllocateBinQuantity {
    public int     BinEntry { get; set; }
    public decimal Quantity { get; set; }
}

public class ReAllocateRequest {
    [Required]
    public required string ItemCode { get; set; }

    /// <summary>Quantities to take out of each source bin, base units.</summary>
    public List<ReAllocateBinQuantity> Sources { get; set; } = [];

    /// <summary>Quantities to put into each target bin (1–3), base units.</summary>
    public List<ReAllocateBinQuantity> Targets { get; set; } = [];
}

public class ReAllocateResponse {
    public bool    Success      { get; set; }
    public string? ErrorMessage { get; set; }
    public int?    SapDocEntry  { get; set; }
    public int?    SapDocNumber { get; set; }
}
