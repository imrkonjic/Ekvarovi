using EKvarovi.Api.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class FaultTypeConfiguration : IEntityTypeConfiguration<FaultType>
{
    public void Configure(EntityTypeBuilder<FaultType> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new FaultType { Id = 1, Name = "Elektrika", IsActive = true, SortOrder = 1 },
            new FaultType { Id = 2, Name = "Voda", IsActive = true, SortOrder = 2 },
            new FaultType { Id = 3, Name = "Grijanje", IsActive = true, SortOrder = 3 },
            new FaultType { Id = 4, Name = "Mreža", IsActive = true, SortOrder = 4 },
            new FaultType { Id = 5, Name = "Građevinski radovi", IsActive = true, SortOrder = 5 },
            new FaultType { Id = 6, Name = "Ostalo", IsActive = true, SortOrder = 6 });
    }
}
