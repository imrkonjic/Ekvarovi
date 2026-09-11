using EKvarovi.Api.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class InterventionStatusConfiguration : IEntityTypeConfiguration<InterventionStatus>
{
    public void Configure(EntityTypeBuilder<InterventionStatus> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new InterventionStatus { Id = 1, Name = "Planirana", IsActive = true, SortOrder = 1 },
            new InterventionStatus { Id = 2, Name = "U tijeku", IsActive = true, SortOrder = 2 },
            new InterventionStatus { Id = 3, Name = "Završena", IsActive = true, SortOrder = 3 },
            new InterventionStatus { Id = 4, Name = "Neuspješna", IsActive = true, SortOrder = 4 });
    }
}
