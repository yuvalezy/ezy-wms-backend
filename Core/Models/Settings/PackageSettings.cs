namespace Core.Models.Settings;

public class PackageSettings {
    public BarcodeSettings Barcode { get; set; } = new();
    public LabelSettings   Label   { get; set; } = new();
}

public class LabelSettings {
    public bool AutoPrint { get; set; }
}

public class BarcodeSettings {
    public string Prefix      { get; set; } = string.Empty;
    public string Suffix      { get; set; } = string.Empty;
    public int    Length      { get; set; }
    public int    StartNumber { get; set; }
}