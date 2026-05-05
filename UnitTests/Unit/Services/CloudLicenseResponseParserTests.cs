using Core.Enums;
using Infrastructure.Services;

namespace UnitTests.Unit.Services;

[TestFixture]
public class CloudLicenseResponseParserTests {
    [Test]
    public void ParseAccountValidationResponse_WithMyezyEnvelope_UnwrapsData() {
        const string json = """
            {
              "success": true,
              "data": {
                "licenseData": {
                  "accountStatus": 1,
                  "expirationDate": null,
                  "paymentCycleDate": null,
                  "demoExpirationDate": null,
                  "inactiveReason": null,
                  "maxAllowedDevices": null,
                  "activeDeviceCount": 2,
                  "additionalData": {}
                },
                "devicesToDeactivate": [],
                "serverTimestamp": "2026-05-05T17:54:15Z"
              },
              "timestamp": "2026-05-05T17:54:15Z"
            }
            """;

        var response = CloudLicenseResponseParser.ParseAccountValidationResponse(json);

        Assert.Multiple(() =>
        {
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("Request completed successfully"));
            Assert.That(response.LicenseData, Is.Not.Null);
            Assert.That(response.LicenseData!.AccountStatus, Is.EqualTo(AccountState.Active));
            Assert.That(response.LicenseData.MaxAllowedDevices, Is.Null);
            Assert.That(response.LicenseData.ActiveDeviceCount, Is.EqualTo(2));
            Assert.That(response.ServerTimestamp, Is.EqualTo(DateTime.Parse("2026-05-05T17:54:15Z").ToUniversalTime()));
        });
    }

    [Test]
    public void ParseAccountValidationResponse_WithFlatMockResponse_RemainsCompatible() {
        const string json = """
            {
              "success": true,
              "message": "Account validation completed",
              "licenseData": {
                "accountStatus": "Active",
                "maxAllowedDevices": 5,
                "activeDeviceCount": 1,
                "additionalData": {}
              },
              "devicesToDeactivate": [],
              "timestamp": "2026-05-05T17:54:15Z"
            }
            """;

        var response = CloudLicenseResponseParser.ParseAccountValidationResponse(json);

        Assert.Multiple(() =>
        {
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("Account validation completed"));
            Assert.That(response.LicenseData, Is.Not.Null);
            Assert.That(response.LicenseData!.AccountStatus, Is.EqualTo(AccountState.Active));
            Assert.That(response.LicenseData.MaxAllowedDevices, Is.EqualTo(5));
            Assert.That(response.ServerTimestamp, Is.EqualTo(DateTime.Parse("2026-05-05T17:54:15Z").ToUniversalTime()));
        });
    }

    [Test]
    public void ParseDeviceEventResponse_WithMyezyEnvelope_UsesNestedMessage() {
        const string json = """
            {
              "success": true,
              "data": {
                "message": "Device event processed successfully"
              },
              "timestamp": "2026-05-05T17:54:15Z"
            }
            """;

        var response = CloudLicenseResponseParser.ParseDeviceEventResponse(json);

        Assert.Multiple(() =>
        {
            Assert.That(response.Success, Is.True);
            Assert.That(response.Message, Is.EqualTo("Device event processed successfully"));
            Assert.That(response.ServerTimestamp, Is.EqualTo(DateTime.Parse("2026-05-05T17:54:15Z").ToUniversalTime()));
        });
    }
}
