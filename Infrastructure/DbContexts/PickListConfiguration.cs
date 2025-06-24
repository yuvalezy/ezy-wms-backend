using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DbContexts;

public class PickListConfiguration : IEntityTypeConfiguration<PickList> {
    public void Configure(EntityTypeBuilder<PickList> builder) {
        // Enums are now stored as integers by default
    }
}