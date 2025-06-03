using Core.Entities;

namespace Core.Models;

public class TransferResponse : Transfer {
    public int?  Progress   { get; set; }
    public bool? IsComplete { get; set; }

    public static TransferResponse FromTransfer(Transfer transfer) {
        return new TransferResponse {
            Id              = transfer.Id,
            Number          = transfer.Number,
            CreatedAt       = transfer.CreatedAt,
            CreatedByUserId = transfer.CreatedByUserId,
            CreatedByUser   = transfer.CreatedByUser,
            UpdatedAt       = transfer.UpdatedAt,
            UpdatedByUserId = transfer.UpdatedByUserId,
            UpdatedByUser   = transfer.UpdatedByUser,
            Deleted         = transfer.Deleted,
            DeletedAt       = transfer.DeletedAt,
            Name            = transfer.Name,
            Comments        = transfer.Comments,
            Date            = transfer.Date,
            Status          = transfer.Status,
            WhsCode         = transfer.WhsCode,
            Lines           = transfer.Lines,
            Progress        = 0,
            IsComplete      = false
        };
    }
}