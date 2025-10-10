using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public class CurriculumLineConfiguration : IEntityTypeConfiguration<StudyPlanEntry>
{
    public void Configure(EntityTypeBuilder<StudyPlanEntry> builder)
    {
        builder.ToTable("CurriculumLines");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Year).IsRequired();
        builder.Property(c => c.Grade).IsRequired().HasMaxLength(10);
        builder.Property(c => c.Subject).IsRequired().HasMaxLength(100);
        builder.Property(c => c.WeeklyBlocks).IsRequired();
    }
}
