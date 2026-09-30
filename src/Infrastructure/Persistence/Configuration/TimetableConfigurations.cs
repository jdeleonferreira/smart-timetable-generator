using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Courses;
using SmartTimetableGenerator.Domain.DayTypes;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.TimetableGeneration;
using SmartTimetableGenerator.Domain.Timetables;
using SmartTimetableGenerator.Domain.TrainingProjects;

namespace SmartTimetableGenerator.Infrastructure.Persistence.Configuration;

public class TimetableConfiguration : AuditableConfiguration<Timetable>
{
    public override void PostConfigure(EntityTypeBuilder<Timetable> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Timetable.NameMaxLength).IsRequired();

        builder.HasOne<AcademicYear>().WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Campus>().WithMany().HasForeignKey(x => x.CampusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AcademicPeriod>().WithMany().HasForeignKey(x => x.AcademicPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StudyPlan>().WithMany().HasForeignKey(x => x.StudyPlanId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lessons)
            .WithOne()
            .HasForeignKey("TimetableId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LessonConfiguration : AuditableConfiguration<Lesson>
{
    public override void PostConfigure(EntityTypeBuilder<Lesson> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Day).HasConversion<int>();
        builder.Ignore(x => x.Placement);

        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Subject>().WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Teacher>().WithMany().HasForeignKey(x => x.TeacherId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Space>().WithMany().HasForeignKey(x => x.SpaceId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);

        // Un curso no puede tener dos clases en la misma franja de un horario
        builder.HasIndex("TimetableId", nameof(Lesson.CourseId), nameof(Lesson.ShiftId), nameof(Lesson.Day), nameof(Lesson.PeriodNumber)).IsUnique();
        builder.HasIndex(x => x.TeacherId);
    }
}

public class GenerationJobConfiguration : AuditableConfiguration<GenerationJob>
{
    public override void PostConfigure(EntityTypeBuilder<GenerationJob> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RequestedBy).HasMaxLength(GenerationJob.RequestedByMaxLength);
        builder.Property(x => x.Message).HasMaxLength(GenerationJob.MessageMaxLength);

        builder.HasOne<Timetable>().WithMany().HasForeignKey(x => x.TimetableId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.Status);
    }
}

public class TrainingProjectConfiguration : AuditableConfiguration<TrainingProject>
{
    public override void PostConfigure(EntityTypeBuilder<TrainingProject> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(TrainingProject.NameMaxLength).IsRequired();
        builder.Property(x => x.ResponsibleName).HasMaxLength(TrainingProject.ResponsibleNameMaxLength);
        builder.Property(x => x.Description).HasMaxLength(TrainingProject.DescriptionMaxLength);

        builder.HasOne<AcademicYear>().WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Campus>().WithMany().HasForeignKey(x => x.CampusId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Teacher>().WithMany().HasForeignKey(x => x.ResponsibleTeacherId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Activities)
            .WithOne()
            .HasForeignKey("TrainingProjectId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Activities).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class ProjectActivityConfiguration : AuditableConfiguration<ProjectActivity>
{
    public override void PostConfigure(EntityTypeBuilder<ProjectActivity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(ProjectActivity.TitleMaxLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(ProjectActivity.DescriptionMaxLength);

        builder.HasOne<DayType>().WithMany().HasForeignKey(x => x.DayTypeId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Date);
    }
}
