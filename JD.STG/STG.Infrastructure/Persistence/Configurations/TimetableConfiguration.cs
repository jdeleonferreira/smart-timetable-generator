// Configurations/TimetableConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;
using STG.Domain.ValueObjects;

public sealed class TimetableConfiguration : IEntityTypeConfiguration<Timetable>
{
    public void Configure(EntityTypeBuilder<Timetable> b)
    {
        b.ToTable("Timetable");
        b.HasKey(x => x.Id);

        b.Property(x => x.SchoolYearId).IsRequired();

        b.HasMany<TimetableEntry>()
         .WithOne()
         .HasForeignKey(e => e.TimetableId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}

// Configurations/TimetableEntryConfiguration.cs
public sealed class TimetableEntryConfiguration : IEntityTypeConfiguration<TimetableEntry>
{
    public void Configure(EntityTypeBuilder<TimetableEntry> e)
    {
        e.ToTable("TimetableEntry");
        e.HasKey(x => x.Id);

        e.Property(x => x.TimetableId).IsRequired();
        e.Property(x => x.SchoolYearId).IsRequired();
        e.Property(x => x.AssignmentId).IsRequired();
        e.Property(x => x.GroupId).IsRequired();
        e.Property(x => x.SubjectId).IsRequired();
        e.Property(x => x.TeacherId).IsRequired();

        // TimeSlot como owned type
        e.OwnsOne(x => x.Slot, ow =>
        {
            ow.Property(p => p.Day).HasColumnName("Day").IsRequired();
            ow.Property(p => p.Block).HasColumnName("Block").IsRequired();
        });

        // Índices para las reglas duras
        e.HasIndex(x => new { x.GroupId, x.Slot.Day, x.Slot.Block })
         .IsUnique()
         .HasDatabaseName("IX_TimetableEntry_Group_Day_Block");

        e.HasIndex(x => new { x.TeacherId, x.Slot.Day, x.Slot.Block })
         .HasDatabaseName("IX_TimetableEntry_Teacher_Day_Block");
    }
}
