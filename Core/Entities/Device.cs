using System;
using Core.Enums;

namespace Core.Entities
{
    public class Device : BaseEntity
    {
        public string DeviceUuid { get; set; } // From frontend localStorage
        public string DeviceName { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DeviceStatus Status { get; set; }
        public string StatusNotes { get; set; }
        public DateTime LastActiveDate { get; set; }
    }
}