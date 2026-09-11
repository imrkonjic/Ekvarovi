using EKvarovi.Api.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200);
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_roles_code");

        builder.HasData(
            new Role { Id = 1, Code = "Admin", Name = "Administrator", Description = "Pun pristup svim podacima i postavkama" },
            new Role { Id = 2, Code = "Manager", Name = "Upravitelj", Description = "Pregled svih prijava, kategorizacija, dodjela i zatvaranje" },
            new Role { Id = 3, Code = "Technician", Name = "Izvršitelj", Description = "Rad na vlastitim nalozima i intervencijama" },
            new Role { Id = 4, Code = "Reporter", Name = "Prijavitelj", Description = "Prijava kvarova i pregled vlastitih prijava" });
    }
}
