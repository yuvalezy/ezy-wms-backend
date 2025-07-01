namespace Core.Models;

public class PackageBarcodeSettings
{
    public string Prefix { get; set; } = "PKG";
    public int Length { get; set; } = 14;
    public string Suffix { get; set; } = "";
    public long StartNumber { get; set; } = 1;
}