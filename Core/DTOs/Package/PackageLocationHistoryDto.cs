using Core.Enums;

namespace Core.DTOs.Package;

public class PackageLocationHistoryDto {
    public          Guid                Id                  { get; set; }
    public          Guid                PackageId           { get; set; }
    public          PackageMovementType MovementType        { get; set; }
    public required string              FromWhsCode         { get; set; }
    public          int?                FromBinEntry        { get; set; }
    public          string?             FromBinCode         { get; set; }
    public required string              ToWhsCode           { get; set; }
    public          int?                ToBinEntry          { get; set; }
    public          string?             ToBinCode           { get; set; }
    public          ObjectType          SourceOperationType { get; set; }
    public          Guid?               SourceOperationId   { get; set; }
    public          Guid                UserId              { get; set; }
    public          DateTime            MovementDate        { get; set; }
    public          string?             Notes               { get; set; }
}