using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> b)
    {
        b.ToTable("Assignments");
        b.HasKey(x => x.Id);

        b.Property(x => x.WeeklyHours).IsRequired();

        b.HasIndex(x => new { x.SchoolYearId, x.GroupId, x.SubjectId, x.TeacherId }).IsUnique();

        b.HasOne<SchoolYear>().WithMany().HasForeignKey(x => x.SchoolYearId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Group>().WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Subject>().WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Teacher>().WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);

        b.ToTable(t => t.HasCheckConstraint("CK_Assignment_WeeklyHours", "WeeklyHours BETWEEN 1 AND 50"));
    }
}