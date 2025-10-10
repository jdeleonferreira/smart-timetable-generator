// Configurations/AssignmentConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> b)
    {
        b.ToTable("Assignment");
        b.HasKey(x => x.Id);

        b.Property(x => x.SchoolYearId).IsRequired();
        b.Property(x => x.GroupId).IsRequired();
        b.Property(x => x.SubjectId).IsRequired();
        b.Property(x => x.TeacherId).IsRequired();
        b.Property(x => x.WeeklyHours).IsRequired();

        b.HasIndex(x => new { x.SchoolYearId, x.GroupId, x.SubjectId }).IsUnique();
    }
}
