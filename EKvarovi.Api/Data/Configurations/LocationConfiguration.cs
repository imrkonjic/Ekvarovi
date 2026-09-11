using EKvarovi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(200).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(10);
        builder.Property(x => x.ContactPerson).HasMaxLength(100);
        builder.Property(x => x.ContactPhone).HasMaxLength(30);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_locations_code");

        builder.HasOne(x => x.LocationType)
            .WithMany()
            .HasForeignKey(x => x.LocationTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
