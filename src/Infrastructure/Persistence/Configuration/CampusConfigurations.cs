using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.DayTypes;
using SmartTimetableGenerator.Domain.Spaces;

namespace SmartTimetableGenerator.Infrastructure.Persistence.Configuration;

public class CampusConfiguration : AuditableConfiguration<Campus>
{
    public override void PostConfigure(EntityTypeBuilder<Campus> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Campus.NameMaxLength).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(Campus.CodeMaxLength);
        builder.Property(x => x.Address).HasMaxLength(Campus.AddressMaxLength);
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasMany(x => x.Shifts)
            .WithOne()
            .HasForeignKey("CampusId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShiftConfiguration : AuditableConfiguration<Shift>
{
    public override void PostConfigure(EntityTypeBuilder<Shift> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(Shift.NameMaxLength).IsRequired();

        // Flags: se guarda como entero (combinación de días)
        builder.Property(x => x.Days).HasConversion<int>();

        builder.HasMany(x => x.BellSchedules)
            .WithOne()
            .HasForeignKey("ShiftId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BellScheduleConfiguration : AuditableConfiguration<BellSchedule>
{
    public override void PostConfigure(EntityTypeBuilder<BellSchedule> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasOne<DayType>()
            .WithMany()
            .HasForeignKey(x => x.DayTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("ShiftId", nameof(BellSchedule.DayTypeId)).IsUnique();

        builder.OwnsMany(x => x.Blocks, b =>
        {
            b.ToJson();
            b.Property(x => x.Label).HasMaxLength(BellBlock.LabelMaxLength);
        });
    }
}

public class SpaceConfiguration : AuditableConfiguration<Space>
{
    public override void PostConfigure(EntityTypeBuilder<Space> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Space.NameMaxLength).IsRequired();

        builder.HasOne<Campus>()
            .WithMany()
            .HasForeignKey(x => x.CampusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CampusId, x.Name }).IsUnique();
    }
}
