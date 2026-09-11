using EKvarovi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class FaultReportConfiguration : IEntityTypeConfiguration<FaultReport>
{
    public void Configure(EntityTypeBuilder<FaultReport> builder)
    {
        builder.Property(x => x.ReportNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ClosingNote).HasMaxLength(1000);

        builder.HasIndex(x => x.ReportNumber).IsUnique().HasDatabaseName("ux_fault_reports_report_number");

        builder.HasIndex(x => x.ReportedAt)
            .IsDescending()
            .HasDatabaseName("ix_fault_reports_reported_at");

        builder.HasIndex(x => x.DueDate)
            .HasDatabaseName("ix_fault_reports_due_date");

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FaultType)
            .WithMany()
            .HasForeignKey(x => x.FaultTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FaultPriority)
            .WithMany()
            .HasForeignKey(x => x.FaultPriorityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FaultStatus)
            .WithMany()
            .HasForeignKey(x => x.FaultStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReportedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ClosedByUser)
            .WithMany()
            .HasForeignKey(x => x.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
