using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class CloudSyncQueueConfiguration : IEntityTypeConfiguration<CloudSyncQueue> {
    public void Configure(EntityTypeBuilder<CloudSyncQueue> builder) {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(x => x.DeviceUuid)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(x => x.RequestPayload)
            .IsRequired();
            
        builder.Property(x => x.LastError)
            .HasMaxLength(1000);
            
        builder.HasIndex(x => new { x.Status, x.NextRetryAt });
        builder.HasIndex(x => x.DeviceUuid);
        builder.HasIndex(x => x.EventType);
    }
}