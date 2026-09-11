using EKvarovi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EKvarovi.Api.Data.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoredFileName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RelativePath).HasMaxLength(400).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_attachments_purpose_range", "purpose BETWEEN 1 AND 3");
            t.HasCheckConstraint(
                "ck_attachments_purpose_target",
                """
                (purpose IN (1, 3) AND fault_report_id IS NOT NULL AND intervention_id IS NULL)
                OR
                (purpose = 2 AND intervention_id IS NOT NULL AND fault_report_id IS NULL)
                """);
        });

        builder.HasOne(x => x.FaultReport)
            .WithMany()
            .HasForeignKey(x => x.FaultReportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Intervention)
            .WithMany()
            .HasForeignKey(x => x.InterventionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.UploadedByUser)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
