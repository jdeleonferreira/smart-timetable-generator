using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Subject"/>.</summary>
public sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.ToTable("Subject");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Code).HasMaxLength(32);
        b.Property(x => x.IsElective).HasDefaultValue(false).IsRequired();

        b.Property(x => x.StudyAreaId).IsRequired();

        b.HasOne(x => x.StudyArea)
         .WithMany(a => a.Subjects)
         .HasForeignKey(x => x.StudyAreaId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.Name).IsUnique();
        b.HasIndex(x => x.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
    }
}
