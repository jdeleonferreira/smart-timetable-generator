using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class StudyPlanEntryConfiguration : IEntityTypeConfiguration<StudyPlanEntry>
{
    public void Configure(EntityTypeBuilder<StudyPlanEntry> b)
    {
        b.ToTable("StudyPlanEntries");
        b.HasKey(x => x.Id);

        b.Property(x => x.WeeklyHours).IsRequired();

        b.HasIndex(x => new { x.StudyPlanId, x.SubjectId }).IsUnique();

        b.HasOne<StudyPlan>()
            .WithMany()
            .HasForeignKey(x => x.StudyPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        b.ToTable(t => t.HasCheckConstraint("CK_SPE_WeeklyHours", "WeeklyHours BETWEEN 1 AND 50"));
    }
}
