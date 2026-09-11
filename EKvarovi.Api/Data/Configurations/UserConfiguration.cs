using EKvarovi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Specialization).HasMaxLength(100);

        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ux_users_email");

        builder.HasOne(x => x.HomeLocation)
            .WithMany(x => x.HomeUsers)
            .HasForeignKey(x => x.HomeLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
