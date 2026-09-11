using EKvarovi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class InterventionConfiguration : IEntityTypeConfiguration<Intervention>
{
    public void Configure(EntityTypeBuilder<Intervention> builder)
    {
        builder.Property(x => x.Note).HasMaxLength(2000);
        builder.Property(x => x.FailureReason).HasMaxLength(500);

        builder.HasIndex(x => x.StartedAt)
            .IsDescending()
            .HasDatabaseName("ix_interventions_started_at");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_interventions_times",
            "finished_at IS NULL OR started_at IS NULL OR finished_at >= started_at"));

        builder.HasOne(x => x.WorkAssignment)
            .WithMany(x => x.Interventions)
            .HasForeignKey(x => x.WorkAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FaultReport)
            .WithMany(x => x.Interventions)
            .HasForeignKey(x => x.FaultReportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Technician)
            .WithMany()
            .HasForeignKey(x => x.TechnicianUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InterventionStatus)
            .WithMany()
            .HasForeignKey(x => x.InterventionStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
