namespace Core.Models;

/// <summary>Result of a one-off SAP Business One Service Layer connection test.</summary>
public sealed record SboConnectionResult(bool Success, string? Message);
