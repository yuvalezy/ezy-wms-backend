using Core.Enums;

namespace Core.DTOs.Package;

public class PackageTransactionDto {
    public          Guid                   Id                    { get; set; }
    public          Guid                   PackageId             { get; set; }
    public          PackageTransactionType TransactionType       { get; set; }
    public required string                 ItemCode              { get; set; }
    public          decimal                Quantity              { get; set; }
    public          UnitType               UnitType              { get; set; }
    public          ObjectType             SourceOperationType   { get; set; }
    public          Guid?                  SourceOperationId     { get; set; }
    public          Guid?                  SourceOperationLineId { get; set; }
    public          UserAuditResponse?     User                  { get; set; }
    public          DateTime               TransactionDate       { get; set; }
    public          string?                Notes                 { get; set; }
}