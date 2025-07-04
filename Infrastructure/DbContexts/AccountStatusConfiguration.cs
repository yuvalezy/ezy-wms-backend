using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class AccountStatusConfiguration : IEntityTypeConfiguration<AccountStatus> {
    public void Configure(EntityTypeBuilder<AccountStatus> builder) {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.InactiveReason)
            .HasMaxLength(500);
            
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.LastValidationTimestamp);
    }
}