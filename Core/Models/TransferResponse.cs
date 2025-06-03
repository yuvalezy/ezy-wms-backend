using Core.Entities;

namespace Core.Models;

public class TransferResponse : Transfer {
    public int?  Progress   { get; set; }
    public bool? IsComplete { get; set; }
}