namespace Core.DTOs.Device
{
    public class UpdateDeviceStatusRequest
    {
        public required string Status { get; set; }
        public required string Reason { get; set; }
    }
}