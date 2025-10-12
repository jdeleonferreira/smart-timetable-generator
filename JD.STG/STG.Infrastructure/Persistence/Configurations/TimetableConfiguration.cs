using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class TimetableConfiguration : IEntityTypeConfiguration<Timetable>
{
    public void Configure(EntityTypeBuilder<Timetable> b)
    {
        b.ToTable("Timetable");
        b.HasKey(x => x.Id);

        b.Property(x => x.GroupId).IsRequired();
        b.Property(x => x.SchoolYearId).IsRequired();
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(500);

        // Unique per (Group, SchoolYear)
        b.HasIndex(x => new { x.GroupId, x.SchoolYearId }).IsUnique();

        b.HasOne(x => x.Group)
         .WithMany()
         .HasForeignKey(x => x.GroupId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SchoolYear)
         .WithMany()
         .HasForeignKey(x => x.SchoolYearId)
         .OnDelete(DeleteBehavior.Restrict);

        b.Navigation(x => x.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
