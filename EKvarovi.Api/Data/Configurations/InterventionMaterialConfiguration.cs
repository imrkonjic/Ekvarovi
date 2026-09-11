using EKvarovi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class InterventionMaterialConfiguration : IEntityTypeConfiguration<InterventionMaterial>
{
    public void Configure(EntityTypeBuilder<InterventionMaterial> builder)
    {
        builder.Property(x => x.Quantity).HasPrecision(10, 2);
        builder.Property(x => x.UnitPriceSnapshot).HasPrecision(10, 2);

        builder.HasIndex(x => new { x.InterventionId, x.MaterialId })
            .IsUnique()
            .HasDatabaseName("ux_intervention_materials_unique_item");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_intervention_materials_quantity",
            "quantity > 0 AND quantity <= 99999.99"));

        builder.HasOne(x => x.Intervention)
            .WithMany()
            .HasForeignKey(x => x.InterventionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Material)
            .WithMany(x => x.InterventionMaterials)
            .HasForeignKey(x => x.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
