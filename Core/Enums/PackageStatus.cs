namespace Core.Enums;

public enum PackageStatus {
    Init      = 0,
    Active    = 1,
    Closed    = 2,
    Cancelled = 3,
    Locked    = 4
}

public enum PackageTransactionType {
    Add      = 0,
    Remove   = 1,
    Transfer = 2,
    Count    = 3
}

public enum PackageMovementType {
    Created     = 0,
    Moved       = 1,
    Transferred = 2
}

public enum InconsistencyType {
    SapStockLessThanWms     = 0,
    PackageExceedsSapStock  = 1,
    NegativePackageQuantity = 2,
    ValidationError         = 3,
    LocationMismatch        = 4,
    DuplicateContent        = 5
}

public enum InconsistencySeverity {
    Low      = 1,
    Medium   = 2,
    High     = 3,
    Critical = 4
}