using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="StudyPlanEntry"/>.</summary>
public sealed class StudyPlanEntryConfiguration : IEntityTypeConfiguration<StudyPlanEntry>
{
    public void Configure(EntityTypeBuilder<StudyPlanEntry> b)
    {
        b.ToTable("StudyPlanEntry");
        b.HasKey(x => x.Id);

        b.Property(x => x.StudyPlanId).IsRequired();
        b.Property(x => x.GradeId).IsRequired();
        b.Property(x => x.SubjectId).IsRequired();
        b.Property(x => x.WeeklyHours).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(250);

        // Unique per plan: (Grade, Subject)
        b.HasIndex(x => new { x.StudyPlanId, x.GradeId, x.SubjectId }).IsUnique();

        // Relationships
        b.HasOne<StudyPlan>()
         .WithMany(p => p.Entries)
         .HasForeignKey(x => x.StudyPlanId)
         .OnDelete(DeleteBehavior.Cascade); // delete plan -> delete entries

        b.HasOne<Grade>()
         .WithMany()
         .HasForeignKey(x => x.GradeId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Subject>()
         .WithMany()
         .HasForeignKey(x => x.SubjectId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
