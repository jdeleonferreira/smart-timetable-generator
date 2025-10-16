// src/STG.Infrastructure/Persistence/Configurations/StudyPlanConfiguration.cs
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

        b.Property(x => x.SchoolYearId).IsRequired();
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(500);

        // One plan per SchoolYear (business rule)
        b.HasIndex(x => x.SchoolYearId).IsUnique();

        // Relationship with SchoolYear (restrict delete to avoid accidental cascades)
        b.HasOne<SchoolYear>()
         .WithMany()
         .HasForeignKey(x => x.SchoolYearId)
         .OnDelete(DeleteBehavior.Restrict);

        // Entries navigation (backing field list in StudyPlan)
        b.Navigation(nameof(StudyPlan.Entries)).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
