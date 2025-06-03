using Core.Entities;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
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
            );
    }
}