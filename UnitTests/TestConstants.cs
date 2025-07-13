using Core.Models;

namespace UnitTests;

public static class TestConstants {
    public static readonly SessionInfo SessionInfo = new() {
        UserId             = "1a90e8b1-ad93-4a12-783b-08ddc2390587",
        Name               = "Administrator",
        SuperUser          = true,
        Warehouse          = "SM",
        EnableBinLocations = true,
        Roles              = [],
        ExpiresAt          = DateTime.UtcNow.AddDays(1),
        Token              = Guid.NewGuid().ToString()
    };
}