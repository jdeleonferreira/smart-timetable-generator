using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;
using STG.Domain.ValueObjects;

namespace STG.Infrastructure.Persistence.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey("GroupCode", "Teacher", "Room", "Slot_Day", "Slot_Block");

        builder.Property(a => a.GroupCode).IsRequired().HasMaxLength(10);
        builder.Property(a => a.Subject).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Teacher).IsRequired().HasMaxLength(120);
        builder.Property(a => a.Room).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Blocks).IsRequired();

        // ----- Mapeo de TimeSlot como owned type (mejor en dos pasos) -----
        var slot = builder.OwnsOne(a => a.Slot);

        // Si tu TimeSlot es record struct con propiedades Day (enum) y Block (int)
        slot.Property(s => s.Day)
            .HasConversion<int>()
            .HasColumnName("Slot_Day")
            .IsRequired();

        slot.Property(s => s.Block)
            .HasColumnName("Slot_Block")
            .IsRequired();

        // Si quieres crear índice opcional:
        // builder.HasIndex("Slot_Day", "Slot_Block");

        // Si Assignment pertenece a Timetable y quieres cascade delete, se configura en TimetableConfiguration
    }
}
