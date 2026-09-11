using EKvarovi.Api.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class LocationTypeConfiguration : IEntityTypeConfiguration<LocationType>
{
    public void Configure(EntityTypeBuilder<LocationType> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new LocationType { Id = 1, Name = "Upravna zgrada", IsActive = true, SortOrder = 1 },
            new LocationType { Id = 2, Name = "Škola", IsActive = true, SortOrder = 2 },
            new LocationType { Id = 3, Name = "Zdravstvena ustanova", IsActive = true, SortOrder = 3 },
            new LocationType { Id = 4, Name = "Skladište", IsActive = true, SortOrder = 4 });
    }
}
