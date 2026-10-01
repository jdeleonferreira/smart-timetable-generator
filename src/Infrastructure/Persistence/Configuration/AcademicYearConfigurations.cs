using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.DayTypes;

namespace SmartTimetableGenerator.Infrastructure.Persistence.Configuration;

public class AcademicYearConfiguration : AuditableConfiguration<AcademicYear>
{
    public override void PostConfigure(EntityTypeBuilder<AcademicYear> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(AcademicYear.NameMaxLength).IsRequired();
        builder.HasIndex(x => x.Year).IsUnique();

        builder.HasMany(x => x.Periods)
            .WithOne()
            .HasForeignKey("AcademicYearId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.CalendarEntries)
            .WithOne()
            .HasForeignKey("AcademicYearId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Periods).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.CalendarEntries).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class AcademicPeriodConfiguration : AuditableConfiguration<AcademicPeriod>
{
    public override void PostConfigure(EntityTypeBuilder<AcademicPeriod> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(AcademicPeriod.NameMaxLength).IsRequired();
    }
}

public class CalendarEntryConfiguration : AuditableConfiguration<CalendarEntry>
{
    public override void PostConfigure(EntityTypeBuilder<CalendarEntry> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Description).HasMaxLength(CalendarEntry.DescriptionMaxLength).IsRequired();

        builder.HasOne<Campus>()
            .WithMany()
            .HasForeignKey(x => x.CampusId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DayType>()
            .WithMany()
            .HasForeignKey(x => x.DayTypeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Date);
    }
}
