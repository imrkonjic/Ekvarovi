using EKvarovi.Api.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class FaultPriorityConfiguration : IEntityTypeConfiguration<FaultPriority>
{
    public void Configure(EntityTypeBuilder<FaultPriority> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ColorHex).HasMaxLength(7).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new FaultPriority { Id = 1, Name = "Nizak", DefaultResolutionHours = 168, ColorHex = "#6c757d", IsActive = true, SortOrder = 1 },
            new FaultPriority { Id = 2, Name = "Srednji", DefaultResolutionHours = 72, ColorHex = "#0d6efd", IsActive = true, SortOrder = 2 },
            new FaultPriority { Id = 3, Name = "Visok", DefaultResolutionHours = 24, ColorHex = "#fd7e14", IsActive = true, SortOrder = 3 },
            new FaultPriority { Id = 4, Name = "Kritičan", DefaultResolutionHours = 4, ColorHex = "#dc3545", IsActive = true, SortOrder = 4 });
    }
}
