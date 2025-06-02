namespace Core.Models.Settings;

public class ConnectionStringsSettings {
    public required string DefaultConnection         { get; set; }
    public required string ExternalAdapterConnection { get; set; }
}

