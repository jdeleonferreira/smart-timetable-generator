using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class StudyPlanConfiguration : IEntityTypeConfiguration<StudyPlan>
{
    public void Configure(EntityTypeBuilder<StudyPlan> b)
    {
        b.ToTable("StudyPlans");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(StudyPlan.MaxNameLength);

        b.HasIndex(x => new { x.SchoolYearId, x.GradeId, x.Name }).IsUnique();

        b.HasOne<SchoolYear>()
            .WithMany()
            .HasForeignKey(x => x.SchoolYearId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Grade>()
            .WithMany()
            .HasForeignKey(x => x.GradeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
