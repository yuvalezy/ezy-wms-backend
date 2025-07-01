namespace Core.DTOs.Package;

public class PackageValidationResult {
    public bool                          IsValid            { get; set; }
    public List<string>                  Errors             { get; set; } = [];
    public List<string>                  Warnings           { get; set; } = [];
    public bool                          HasInconsistencies { get; set; }
    public List<PackageInconsistencyDto> Inconsistencies    { get; set; } = [];
}