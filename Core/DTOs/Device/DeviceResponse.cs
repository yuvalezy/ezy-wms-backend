using System;

namespace Core.DTOs.Device
{
    public class DeviceResponse
    {
        public Guid Id { get; set; }
        public required string DeviceUuid { get; set; }
        public required string DeviceName { get; set; }
        public required string Status { get; set; }
        public DateTime RegistrationDate { get; set; }
        public required string StatusNotes { get; set; }
        public DateTime? LastActiveDate { get; set; }
    }
}