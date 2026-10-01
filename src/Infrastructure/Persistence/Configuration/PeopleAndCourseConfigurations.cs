using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Courses;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.TeachingAssignments;

namespace SmartTimetableGenerator.Infrastructure.Persistence.Configuration;

public class TeacherConfiguration : AuditableConfiguration<Teacher>
{
    public override void PostConfigure(EntityTypeBuilder<Teacher> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FirstName).HasMaxLength(Teacher.NameMaxLength).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(Teacher.NameMaxLength).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(Teacher.EmailMaxLength);
        builder.Property(x => x.Phone).HasMaxLength(Teacher.PhoneMaxLength);
        builder.Property(x => x.UserId).HasMaxLength(Teacher.UserIdMaxLength);
        builder.Ignore(x => x.FullName);

        builder.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        builder.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");

        builder.OwnsMany(x => x.Areas, a =>
        {
            a.ToTable("TeacherAreas");
            a.WithOwner().HasForeignKey("TeacherId");
            a.HasKey("TeacherId", nameof(TeacherArea.AreaId));
        });

        builder.OwnsMany(x => x.Campuses, c =>
        {
            c.ToTable("TeacherCampuses");
            c.WithOwner().HasForeignKey("TeacherId");
            c.HasKey("TeacherId", nameof(TeacherCampus.CampusId));
        });

        builder.OwnsMany(x => x.Availability, a => a.ToJson());
    }
}

public class CourseConfiguration : AuditableConfiguration<Course>
{
    public override void PostConfigure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Course.NameMaxLength).IsRequired();

        builder.HasOne<AcademicYear>().WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Campus>().WithMany().HasForeignKey(x => x.CampusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Grade>().WithMany().HasForeignKey(x => x.GradeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Space>().WithMany().HasForeignKey(x => x.HomeRoomId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Teacher>().WithMany().HasForeignKey(x => x.HomeroomTeacherId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AcademicYearId, x.CampusId, x.GradeId, x.Name }).IsUnique();
    }
}

public class TeachingAssignmentConfiguration : AuditableConfiguration<TeachingAssignment>
{
    public override void PostConfigure(EntityTypeBuilder<TeachingAssignment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne<AcademicYear>().WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Subject>().WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AcademicPeriod>().WithMany().HasForeignKey(x => x.AcademicPeriodId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Teacher>().WithMany().HasForeignKey(x => x.TeacherId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CourseId, x.SubjectId, x.AcademicPeriodId });
        builder.HasIndex(x => x.TeacherId);
    }
}
