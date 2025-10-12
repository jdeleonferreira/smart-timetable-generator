using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Area"/> aggregate root.
/// </summary>
public sealed class StudyAreaConfiguration : IEntityTypeConfiguration<StudyArea>
{
    public void Configure(EntityTypeBuilder<StudyArea> b)
    {
        b.ToTable("StudyAreas");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Code).HasMaxLength(32);
        b.Property(x => x.OrderNo).IsRequired();
        b.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();

        b.HasIndex(x => x.Name).IsUnique();
        b.HasIndex(x => x.Code).IsUnique().HasFilter("[Code] IS NOT NULL");

        b.Navigation(x => x.Subjects).UsePropertyAccessMode(PropertyAccessMode.Field);

        //b.HasMany(x => x.Subjects)
        // .WithOne(s => s.StudyArea)
        // .HasForeignKey(s => s.StudyAreaId)
        // .OnDelete(DeleteBehavior.Restrict);
    }
}
