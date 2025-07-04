using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.Property(e => e.DeviceUuid)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(e => e.DeviceName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(e => e.DeviceName)
            .IsUnique();
        
        builder.Property(e => e.StatusNotes)
            .HasMaxLength(500);
            
        builder.HasIndex(e => e.DeviceUuid)
            .IsUnique();
    }
}