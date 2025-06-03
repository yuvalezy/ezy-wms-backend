using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class PickListConfiguration : IEntityTypeConfiguration<PickList> {
    public void Configure(EntityTypeBuilder<PickList> builder) {
        // Configure enum conversion for char storage
        builder.Property(e => e.Status)
            .HasConversion<char>();
    }
}