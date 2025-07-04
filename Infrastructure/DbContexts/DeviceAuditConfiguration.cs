using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts
{
    public class DeviceAuditConfiguration : IEntityTypeConfiguration<DeviceAudit>
    {
        public void Configure(EntityTypeBuilder<DeviceAudit> builder)
        {
            builder.Property(e => e.Reason)
                .HasMaxLength(500);
            
            builder.HasOne(e => e.Device)
                .WithMany()
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}