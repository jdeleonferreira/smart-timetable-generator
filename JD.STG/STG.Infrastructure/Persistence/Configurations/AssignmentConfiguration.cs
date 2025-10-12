// src/STG.Infrastructure/Persistence/Configurations/AssignmentConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> b)
    {
        b.ToTable("Assignment");
        b.HasKey(x => x.Id);

        b.Property(x => x.GroupId).IsRequired();
        b.Property(x => x.SubjectId).IsRequired();
        b.Property(x => x.SchoolYearId).IsRequired();
        b.Property(x => x.WeeklyHours).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(300);

        // Uniqueness per (Group, Subject, Year)
        b.HasIndex(x => new { x.GroupId, x.SubjectId, x.SchoolYearId }).IsUnique();

        // Relationships (restrict deletes to avoid accidental cascades)
        b.HasOne(x => x.Group)
         .WithMany()
         .HasForeignKey(x => x.GroupId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Subject)
         .WithMany()
         .HasForeignKey(x => x.SubjectId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SchoolYear)
         .WithMany()
         .HasForeignKey(x => x.SchoolYearId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Teacher>()
         .WithMany()
         .HasForeignKey(x => x.TeacherId)
         .OnDelete(DeleteBehavior.SetNull);
    }
}
