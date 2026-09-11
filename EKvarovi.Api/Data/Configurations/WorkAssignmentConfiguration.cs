using EKvarovi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class WorkAssignmentConfiguration : IEntityTypeConfiguration<WorkAssignment>
{
    public void Configure(EntityTypeBuilder<WorkAssignment> builder)
    {
        builder.Property(x => x.ReassignReason).HasMaxLength(500);
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasIndex(x => x.FaultReportId)
            .HasFilter("is_active AND NOT is_deleted")
            .IsUnique()
            .HasDatabaseName("ux_work_assignments_active_per_report");

        builder.HasIndex(x => new { x.TechnicianUserId, x.IsActive })
            .HasDatabaseName("ix_work_assignments_technician");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_work_assignments_unassigned",
            "is_active = false OR unassigned_at IS NULL"));

        builder.HasOne(x => x.FaultReport)
            .WithMany(x => x.WorkAssignments)
            .HasForeignKey(x => x.FaultReportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Technician)
            .WithMany()
            .HasForeignKey(x => x.TechnicianUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedByUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
