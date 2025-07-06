using Core.Entities;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class AccountStatusConfiguration : IEntityTypeConfiguration<AccountStatus> {
    public void Configure(EntityTypeBuilder<AccountStatus> builder) {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedNever(); // Singleton with fixed ID = 1
        
        builder.Property(x => x.InactiveReason)
            .HasMaxLength(500);
            
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.LastValidationTimestamp);
        
        // Seed the singleton record
        builder.HasData(new AccountStatus {
            Id = 1,
            Status = AccountState.Invalid,
            LastValidationTimestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}