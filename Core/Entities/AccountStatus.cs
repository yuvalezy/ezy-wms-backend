using Core.Enums;

namespace Core.Entities;

public class AccountStatus : BaseEntity {
    public AccountState Status                 { get; set; }
    public DateTime?    ExpirationDate        { get; set; }
    public DateTime?    PaymentCycleDate      { get; set; }
    public DateTime?    DemoExpirationDate    { get; set; }
    public string?      InactiveReason        { get; set; }
    public DateTime     LastValidationTimestamp { get; set; }
}