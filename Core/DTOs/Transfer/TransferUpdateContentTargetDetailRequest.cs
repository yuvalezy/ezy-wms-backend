using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Transfer;

public class TransferUpdateContentTargetDetailRequest {
    [Required]
    public Guid ID { get; set; }

    public List<Guid>? RemoveRows { get; set; }

    public Dictionary<Guid, decimal>? QuantityChanges { get; set; }
}