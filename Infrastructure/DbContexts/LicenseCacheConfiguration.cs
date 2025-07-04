using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class LicenseCacheConfiguration : IEntityTypeConfiguration<LicenseCache> {
    public void Configure(EntityTypeBuilder<LicenseCache> builder) {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.EncryptedData)
            .IsRequired();
            
        builder.Property(x => x.DataHash)
            .HasMaxLength(256);
            
        builder.HasIndex(x => x.ExpirationTimestamp);
        builder.HasIndex(x => x.CacheTimestamp);
    }
}