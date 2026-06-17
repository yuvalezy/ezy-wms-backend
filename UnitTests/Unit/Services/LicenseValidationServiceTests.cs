using Core.Entities;
using Core.Enums;
using Core.Models;
using Core.Services;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Unit.Services;

/// <summary>
/// Regression cover for the WMS misreporting a live demo license as "Expired".
/// MyEzy carries a demo license's expiry in <c>demoExpirationDate</c> (with
/// <c>expirationDate</c> null). Once that date is persisted on the AccountStatus
/// entity, GetLicenseValidationResultAsync must coalesce it and produce a positive
/// days-until-expiration — never 0/"Expired" — for a future date.
/// </summary>
[TestFixture]
public class LicenseValidationServiceTests {
    private static LicenseValidationService BuildSut(AccountStatus status) =>
        new(new StubAccountStatusService(status),
            new StubDeviceService(),
            NullLogger<LicenseValidationService>.Instance);

    [Test]
    public async Task GetLicenseValidationResult_DemoWithFutureDemoExpiry_IsNotExpired() {
        var demoExpiry = DateTime.UtcNow.AddDays(18);
        var sut = BuildSut(new AccountStatus {
            Status             = AccountState.Demo,
            ExpirationDate     = null,        // demo keys leave this null...
            DemoExpirationDate = demoExpiry   // ...and carry the expiry here
        });

        var result = await sut.GetLicenseValidationResultAsync();

        Assert.Multiple(() => {
            Assert.That(result.AccountStatus, Is.EqualTo(AccountState.Demo));
            Assert.That(result.IsValid, Is.True);
            // The coalesced date is the demo expiry, and the countdown is positive.
            Assert.That(result.ExpirationDate, Is.EqualTo(demoExpiry));
            Assert.That(result.DaysUntilExpiration, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task GetLicenseValidationResult_NoDate_DoesNotInventAnExpiry() {
        // With no date at all the countdown is genuinely unknown (0), and the
        // frontend renders "Unknown" rather than "Expired".
        var sut = BuildSut(new AccountStatus {
            Status             = AccountState.Demo,
            ExpirationDate     = null,
            DemoExpirationDate = null
        });

        var result = await sut.GetLicenseValidationResultAsync();

        Assert.Multiple(() => {
            Assert.That(result.ExpirationDate, Is.Null);
            Assert.That(result.DaysUntilExpiration, Is.EqualTo(0));
        });
    }

    // --- minimal hand-rolled stubs (the project has no mocking library) ----------

    private sealed class StubAccountStatusService(AccountStatus status) : IAccountStatusService {
        public Task<AccountStatus> GetCurrentAccountStatusAsync() => Task.FromResult(status);
        public Task<AccountStatus> UpdateAccountStatusAsync(AccountState newStatus, string reason) => throw new NotImplementedException();
        public Task<AccountStatus> SyncFromCloudAsync(LicenseData licenseData, string reason) => throw new NotImplementedException();
        public Task<bool> IsAccountActiveAsync() => throw new NotImplementedException();
        public Task<bool> IsPaymentDueAsync() => throw new NotImplementedException();
        public Task<bool> IsSystemAccessAllowedAsync() => throw new NotImplementedException();
        public Task<List<AccountStatusAudit>> GetStatusHistoryAsync() => throw new NotImplementedException();
        public Task ProcessAccountStatusTransitionAsync() => throw new NotImplementedException();
    }

    private sealed class StubDeviceService : IDeviceService {
        public Task<Device> RegisterDeviceAsync(string deviceUuid, string deviceName, SessionInfo sessionInfo) => throw new NotImplementedException();
        public Task<Device?> GetDeviceAsync(string deviceUuid) => throw new NotImplementedException();
        public Task<bool> ValidateDeviceNameAvailable(string name) => throw new NotImplementedException();
        public Task<List<Device>> GetAllDevicesAsync(DeviceStatus? status = null, string? searchTerm = null) => throw new NotImplementedException();
        public Task<Device> UpdateDeviceStatusAsync(string deviceUuid, DeviceStatus status, string reason, SessionInfo? sessionInfo) => throw new NotImplementedException();
        public Task<Device> UpdateDeviceNameAsync(string deviceUuid, string newName, SessionInfo sessionInfo) => throw new NotImplementedException();
        public Task<List<DeviceAudit>> GetDeviceAuditHistoryAsync(string deviceUuid) => throw new NotImplementedException();
        public Task<bool> IsDeviceActiveAsync(string deviceUuid) => throw new NotImplementedException();
        public Task RecordDeviceLoginAsync(string deviceUuid) => throw new NotImplementedException();
    }
}
