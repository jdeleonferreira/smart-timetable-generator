using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class TimetableEntryConfiguration : IEntityTypeConfiguration<TimetableEntry>
{
    public void Configure(EntityTypeBuilder<TimetableEntry> b)
    {
        b.ToTable("TimetableEntry");
        b.HasKey(x => x.Id);

        b.Property(x => x.TimetableId).IsRequired();
        b.Property(x => x.AssignmentId).IsRequired();
        b.Property(x => x.DayOfWeek).IsRequired();
        b.Property(x => x.PeriodIndex).IsRequired();
        b.Property(x => x.Span).IsRequired();
        b.Property(x => x.Room).HasMaxLength(40);
        b.Property(x => x.Notes).HasMaxLength(250);

        // No overlap in the same timetable at (Day, Period)
        b.HasIndex(x => new { x.TimetableId, x.DayOfWeek, x.PeriodIndex }).IsUnique();

        // Relations
        b.HasOne(x => x.Timetable)
         .WithMany(t => t.Entries)
         .HasForeignKey(x => x.TimetableId)
         .OnDelete(DeleteBehavior.Cascade); // delete timetable -> delete entries

        b.HasOne(x => x.Assignment)
         .WithMany()
         .HasForeignKey(x => x.AssignmentId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}