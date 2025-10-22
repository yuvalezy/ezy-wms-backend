using Core.Enums;

namespace Core.Models.Settings;

public class PresenceTrackingSettings {
    public SessionManagementType Type { get; set; } = SessionManagementType.InMemory;
}
