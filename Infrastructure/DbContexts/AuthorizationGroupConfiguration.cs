using Core.Entities;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class AuthorizationGroupConfiguration: IEntityTypeConfiguration<AuthorizationGroup> {
    public void Configure(EntityTypeBuilder<AuthorizationGroup> builder) {
        // Store the Authorization enum collection as a JSON string
        builder.Property(e => e.Authorizations)
            .HasConversion(
                v => string.Join(',', v.Select(a => (int)a)),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => (RoleType)int.Parse(s))
                    .ToList()
            )
            .Metadata.SetValueComparer(
                new ValueComparer<ICollection<RoleType>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList())
            );
    }
}