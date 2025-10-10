using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class TimetableEntryConfiguration : IEntityTypeConfiguration<TimetableEntry>
{
    public void Configure(EntityTypeBuilder<TimetableEntry> b)
    {
        b.ToTable("TimetableEntries");
        b.HasKey(x => x.Id);

        b.HasIndex(x => new { x.TimetableId, x.DayOfWeek, x.PeriodNumber }).IsUnique();

        b.HasOne<Timetable>()
            .WithMany()
            .HasForeignKey(x => x.TimetableId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<Assignment>()
            .WithMany()
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Room>()
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        b.ToTable(t => t.HasCheckConstraint("CK_TTE_PeriodNumber", "PeriodNumber BETWEEN 1 AND 20"));
    }
}
