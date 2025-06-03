using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class UserConfiguration: IEntityTypeConfiguration<User> {
    public void Configure(EntityTypeBuilder<User> builder) {
        builder
            .HasOne<AuthorizationGroup>(v => v.AuthorizationGroup)
            .WithMany()
            .HasForeignKey(a => a.AuthorizationGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Warehouses collection as a JSON string
        builder.Property(e => e.Warehouses)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            )
            .Metadata.SetValueComparer(
                new ValueComparer<ICollection<string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList())
            );
            
        builder.Property(e => e.Warehouses)
            .HasMaxLength(1000); // Adjust based on expected number of warehouses
    }
}