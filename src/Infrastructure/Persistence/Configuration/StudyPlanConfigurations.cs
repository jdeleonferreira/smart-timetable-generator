using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Infrastructure.Persistence.Configuration;

public class StudyPlanConfiguration : AuditableConfiguration<StudyPlan>
{
    public override void PostConfigure(EntityTypeBuilder<StudyPlan> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(StudyPlan.NameMaxLength).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(StudyPlan.NotesMaxLength);

        builder.HasOne<AcademicYear>()
            .WithMany()
            .HasForeignKey(x => x.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Campus>()
            .WithMany()
            .HasForeignKey(x => x.CampusId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un plan por sede y año lectivo
        builder.HasIndex(x => new { x.AcademicYearId, x.CampusId }).IsUnique();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("StudyPlanId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StudyPlanItemConfiguration : AuditableConfiguration<StudyPlanItem>
{
    public override void PostConfigure(EntityTypeBuilder<StudyPlanItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Note).HasMaxLength(StudyPlanItem.NoteMaxLength);

        builder.HasOne<Grade>()
            .WithMany()
            .HasForeignKey(x => x.GradeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(x => x.IntegratedIntoSubjectId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Shift>()
            .WithMany()
            .HasForeignKey(x => x.TargetShiftId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("StudyPlanId", nameof(StudyPlanItem.GradeId), nameof(StudyPlanItem.SubjectId)).IsUnique();

        builder.OwnsMany(x => x.PeriodHours, p => p.ToJson());
    }
}
