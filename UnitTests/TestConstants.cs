using Core.Models;

namespace UnitTests;

public static class TestConstants {
    public static readonly SessionInfo SessionInfo = new() {
        UserId             = "99ee613b-5d76-4479-b140-08ddacd8b635",
        Name               = "Administrator",
        SuperUser          = true,
        Warehouse          = "SM",
        EnableBinLocations = true,
        Roles              = [],
        ExpiresAt          = DateTime.UtcNow.AddDays(1),
        Token              = Guid.NewGuid().ToString()
    };
}