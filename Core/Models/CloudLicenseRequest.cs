using System.Text.Json.Serialization;
using Core.Enums;

namespace Core.Models;

public class CloudLicenseRequest {
    public          string                     DeviceUuid     { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required CloudLicenseEvent          Event          { get; set; }
    public          string                     DeviceName     { get; set; } = string.Empty;
    public          DateTime                   Timestamp      { get; set; }
    public          Dictionary<string, object> AdditionalData { get; set; } = new();
}