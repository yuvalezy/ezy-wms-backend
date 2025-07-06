using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class AccountStatusAuditConfiguration : IEntityTypeConfiguration<AccountStatusAudit> {
    public void Configure(EntityTypeBuilder<AccountStatusAudit> builder) {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Reason)
            .HasMaxLength(500);
            
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.PreviousStatus);
        builder.HasIndex(x => x.NewStatus);
    }
}