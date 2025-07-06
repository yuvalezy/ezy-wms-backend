namespace Core.Enums;

public enum AccountState {
    Invalid           = 0,
    Active            = 1,
    PaymentDue        = 2,
    PaymentDueUnknown = 3,
    Disabled          = 4,
    Demo              = 5,
    DemoExpired       = 6,
}