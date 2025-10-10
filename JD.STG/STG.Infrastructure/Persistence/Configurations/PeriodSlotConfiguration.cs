using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class PeriodSlotConfiguration : IEntityTypeConfiguration<PeriodSlot>
{
    public void Configure(EntityTypeBuilder<PeriodSlot> b)
    {
        b.ToTable("PeriodSlots");
        b.HasKey(x => x.Id);

        b.Property(x => x.Label).HasMaxLength(PeriodSlot.MaxLabelLength);

        b.HasIndex(x => new { x.SchoolYearId, x.DayOfWeek, x.PeriodNumber }).IsUnique();

        b.HasOne<SchoolYear>()
            .WithMany()
            .HasForeignKey(x => x.SchoolYearId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional CHECKs (SQL Server)
        b.ToTable(t => t.HasCheckConstraint("CK_PeriodSlot_PeriodNumber", "PeriodNumber >= 1 AND PeriodNumber <= 20"));
        b.ToTable(t => t.HasCheckConstraint("CK_PeriodSlot_TimeRange", "(StartTime IS NULL OR EndTime IS NULL) OR (StartTime < EndTime)"));
    }
}
