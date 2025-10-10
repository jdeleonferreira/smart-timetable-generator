using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.ToTable("Subjects");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Subject.MaxNameLength);

        b.Property(x => x.Code)
            .HasMaxLength(Subject.MaxCodeLength);

        b.HasIndex(x => new { x.SchoolYearId, x.Name }).IsUnique();
        // Unique filtered index for Code when not null (SQL Server syntax)
        b.HasIndex(x => new { x.SchoolYearId, x.Code }).IsUnique().HasFilter("[Code] IS NOT NULL");

        b.HasOne<SchoolYear>()
            .WithMany()
            .HasForeignKey(x => x.SchoolYearId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
