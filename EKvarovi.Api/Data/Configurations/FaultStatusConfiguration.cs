using EKvarovi.Api.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class FaultStatusConfiguration : IEntityTypeConfiguration<FaultStatus>
{
    public void Configure(EntityTypeBuilder<FaultStatus> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new FaultStatus { Id = 1, Name = "Zaprimljeno", IsClosedState = false, IsActive = true, SortOrder = 1 },
            new FaultStatus { Id = 2, Name = "Pregledano", IsClosedState = false, IsActive = true, SortOrder = 2 },
            new FaultStatus { Id = 3, Name = "Dodijeljeno", IsClosedState = false, IsActive = true, SortOrder = 3 },
            new FaultStatus { Id = 4, Name = "U radu", IsClosedState = false, IsActive = true, SortOrder = 4 },
            new FaultStatus { Id = 5, Name = "Riješeno", IsClosedState = false, IsActive = true, SortOrder = 5 },
            new FaultStatus { Id = 6, Name = "Zatvoreno", IsClosedState = true, IsActive = true, SortOrder = 6 });
    }
}
