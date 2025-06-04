using Core.Entities;
using Core.Enums;

namespace Core.DTOs;

public class TransferResponse {
    public Guid         Id              { get; set; }
    public int          Number          { get; set; }
    public DateTime     CreatedAt       { get; set; }
    public Guid?        CreatedByUserId { get; set; }
    public User?        CreatedByUser   { get; set; }
    public DateTime?    UpdatedAt       { get; set; }
    public Guid?        UpdatedByUserId { get; set; }
    public User?        UpdatedByUser   { get; set; }
    public bool         Deleted         { get; set; }
    public DateTime?    DeletedAt       { get; set; }
    public string?      Name            { get; set; }
    public string?      Comments        { get; set; }
    public DateTime     Date            { get; set; }
    public ObjectStatus Status          { get; set; }
    public int?         Progress        { get; set; }
    public bool?        IsComplete      { get; set; }

    public required string WhsCode { get; set; }

    public IEnumerable<TransferLineResponse> Lines { get; set; } = new List<TransferLineResponse>();

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
            Lines           = transfer.Lines.Select(l => TransferLineResponse.FromTransferLine(l)),
            Progress        = 0,
            IsComplete      = false
        };
    }
}