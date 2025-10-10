// Configurations/StudyPlanEntryConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

public sealed class StudyPlanEntryConfiguration : IEntityTypeConfiguration<StudyPlanEntry>
{
    public void Configure(EntityTypeBuilder<StudyPlanEntry> b)
    {
        b.ToTable("StudyPlanEntry");
        b.HasKey(x => x.Id);

        b.Property(x => x.SchoolYearId).IsRequired();
        b.Property(x => x.GradeId).IsRequired();
        b.Property(x => x.SubjectId).IsRequired();
        b.Property(x => x.WeeklyHours).IsRequired();

        b.HasIndex(x => new { x.SchoolYearId, x.GradeId, x.SubjectId }).IsUnique();
    }
}