
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;


public class PeriodSlotConfiguration : IEntityTypeConfiguration<PeriodSlot>
{
    public void Configure(EntityTypeBuilder<PeriodSlot> b)
    {
        b.ToTable("PeriodSlots");

        b.HasKey(x => x.Id);

        b.Property(x => x.DayOfWeek).IsRequired();
        b.Property(x => x.PeriodNumber).IsRequired();

        b.Property(x => x.StartTime)
            .HasConversion(
                v => v.ToTimeSpan(),
                v => TimeOnly.FromTimeSpan(v))
            .IsRequired();

        b.Property(x => x.EndTime)
            .HasConversion(
                v => v.ToTimeSpan(),
                v => TimeOnly.FromTimeSpan(v))
            .IsRequired();

        b.Property(x => x.IsBreak).HasDefaultValue(false);
        b.Property(x => x.Label).HasMaxLength(64);

        b.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Unicidad por SchoolYear + Día + Periodo
        b.HasIndex(x => new { x.SchoolYearId, x.DayOfWeek, x.PeriodNumber })
         .IsUnique();
    }
}