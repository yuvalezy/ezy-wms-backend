using Core.Models;

namespace UnitTests;

public static class TestConstants {
    public static readonly SessionInfo SessionInfo = new() {
        UserId             = "c9232a3c-7354-4830-1ee2-08ddc7b7a5f1",
        Name               = "Administrator",
        SuperUser          = true,
        Warehouse          = "SM",
        EnableBinLocations = true,
        Roles              = [],
        ExpiresAt          = DateTime.UtcNow.AddDays(1),
        Token              = Guid.NewGuid().ToString()
    };
}