using EKvarovi.Api.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class MaterialUnitConfiguration : IEntityTypeConfiguration<MaterialUnit>
{
    public void Configure(EntityTypeBuilder<MaterialUnit> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Abbreviation).HasMaxLength(10).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new MaterialUnit { Id = 1, Name = "Komad", Abbreviation = "kom", IsActive = true, SortOrder = 1 },
            new MaterialUnit { Id = 2, Name = "Metar", Abbreviation = "m", IsActive = true, SortOrder = 2 },
            new MaterialUnit { Id = 3, Name = "Litra", Abbreviation = "l", IsActive = true, SortOrder = 3 },
            new MaterialUnit { Id = 4, Name = "Kilogram", Abbreviation = "kg", IsActive = true, SortOrder = 4 },
            new MaterialUnit { Id = 5, Name = "Paket", Abbreviation = "pak", IsActive = true, SortOrder = 5 });
    }
}
