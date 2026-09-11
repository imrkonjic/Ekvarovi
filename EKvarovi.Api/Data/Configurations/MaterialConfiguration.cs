using EKvarovi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(10, 2);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_materials_code");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_materials_unit_price",
            "unit_price >= 0"));

        builder.HasOne(x => x.MaterialUnit)
            .WithMany()
            .HasForeignKey(x => x.MaterialUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
