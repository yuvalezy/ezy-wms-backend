namespace Core.DTOs.Device
{
    public class RegisterDeviceRequest
    {
        public required string DeviceUuid { get; set; }
        public required string DeviceName { get; set; }
    }
}