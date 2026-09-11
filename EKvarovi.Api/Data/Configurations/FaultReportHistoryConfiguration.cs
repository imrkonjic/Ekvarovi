using EKvarovi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class FaultReportHistoryConfiguration : IEntityTypeConfiguration<FaultReportHistory>
{
    public void Configure(EntityTypeBuilder<FaultReportHistory> builder)
    {
        builder.Property(x => x.OldValue).HasMaxLength(200);
        builder.Property(x => x.NewValue).HasMaxLength(200);
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasIndex(x => new { x.FaultReportId, x.ChangedAt })
            .HasDatabaseName("ix_histories_report_changed");

        builder.HasOne(x => x.FaultReport)
            .WithMany()
            .HasForeignKey(x => x.FaultReportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ChangedByUser)
            .WithMany()
            .HasForeignKey(x => x.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
