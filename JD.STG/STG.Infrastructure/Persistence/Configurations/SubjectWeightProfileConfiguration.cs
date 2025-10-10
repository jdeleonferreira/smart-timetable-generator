using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class SubjectWeightProfileConfiguration : IEntityTypeConfiguration<SubjectWeightProfile>
{
    public void Configure(EntityTypeBuilder<SubjectWeightProfile> b)
    {
        b.ToTable("SubjectWeightProfiles");
        b.HasKey(x => x.Id);

        b.Property(x => x.Notes).HasMaxLength(256);

        // Unique pair; allow null GradeId (filtered index)
        b.HasIndex(x => new { x.SubjectId, x.GradeId }).IsUnique();

        b.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<Grade>()
            .WithMany()
            .HasForeignKey(x => x.GradeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.ToTable(t => t.HasCheckConstraint("CK_SWP_Energy", "Energy BETWEEN 0 AND 100"));
        b.ToTable(t => t.HasCheckConstraint("CK_SWP_Effort", "Effort BETWEEN 0 AND 100"));
        b.ToTable(t => t.HasCheckConstraint("CK_SWP_Focus", "Focus  BETWEEN 0 AND 100"));
    }
}
