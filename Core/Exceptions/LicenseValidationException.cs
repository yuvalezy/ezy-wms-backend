namespace Core.Exceptions;

public class LicenseValidationException : Exception {
    public string LicenseStatus { get; }

    public LicenseValidationException(string message, string licenseStatus) : base(message) {
        LicenseStatus = licenseStatus;
    }

    public LicenseValidationException(string message, string licenseStatus, Exception innerException) 
        : base(message, innerException) {
        LicenseStatus = licenseStatus;
    }
}