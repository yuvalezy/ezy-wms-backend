using Core.Enums;

namespace Core.DTOs.Package;

public class PackageInconsistencyDto {
    public Guid                  Id                { get; set; }
    public Guid                  PackageId         { get; set; }
    public string?               PackageBarcode    { get; set; }
    public string?               ItemCode          { get; set; }
    public string?               BatchNo           { get; set; }
    public string?               SerialNo          { get; set; }
    public string?               WhsCode           { get; set; }
    public string?               BinCode           { get; set; }
    public decimal?              SapQuantity       { get; set; }
    public decimal?              WmsQuantity       { get; set; }
    public decimal?              PackageQuantity   { get; set; }
    public InconsistencyType     InconsistencyType { get; set; }
    public InconsistencySeverity Severity          { get; set; }
    public DateTime              DetectedAt        { get; set; }
    public bool                  IsResolved        { get; set; }
    public DateTime?             ResolvedAt        { get; set; }
    public string?               ResolvedBy        { get; set; }
    public string?               ResolutionAction  { get; set; }
    public string?               ErrorMessage      { get; set; }
    public string?               Notes             { get; set; }
}