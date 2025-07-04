using System;
using Core.Enums;

namespace Core.Entities
{
    public class DeviceAudit : BaseEntity
    {
        public Guid DeviceId { get; set; }
        public DeviceStatus PreviousStatus { get; set; }
        public DeviceStatus NewStatus { get; set; }
        public string Reason { get; set; }
        
        public virtual Device Device { get; set; }
    }
}