using Core.Enums;

namespace Core.Entities;

public class Device : BaseEntity {
    public string       DeviceUuid       { get; set; } = string.Empty; // From frontend localStorage
    public string       DeviceName       { get; set; } = string.Empty;
    public DateTime     RegistrationDate { get; set; }
    public DeviceStatus Status           { get; set; }
    public string?       StatusNotes      { get; set; }
    public DateTime     LastActiveDate   { get; set; }
}