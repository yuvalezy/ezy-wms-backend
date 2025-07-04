using Core.DTOs.General;
using Core.Enums;

namespace Core.DTOs.License;

public record LicenseWarning(LicenseWarningType Type, params object[] Data);