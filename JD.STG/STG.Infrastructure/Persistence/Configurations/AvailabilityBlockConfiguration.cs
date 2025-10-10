using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class AvailabilityBlockConfiguration : IEntityTypeConfiguration<AvailabilityBlock>
{
    public void Configure(EntityTypeBuilder<AvailabilityBlock> b)
    {
        b.ToTable("AvailabilityBlocks");
        b.HasKey(x => x.Id);

        b.HasIndex(x => new { x.TeacherId, x.DayOfWeek });

        b.HasOne<Teacher>()
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        b.ToTable(t => t.HasCheckConstraint("CK_AB_FromTo", "PeriodFrom >= 1 AND PeriodFrom <= 20 AND PeriodTo >= 1 AND PeriodTo <= 20 AND PeriodFrom <= PeriodTo"));
    }
}
