// FILE: STG.Infrastructure/Persistence/Configurations/RunHistoryConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class RunHistoryConfiguration : IEntityTypeConfiguration<RunHistory>
{
    public void Configure(EntityTypeBuilder<RunHistory> b)
    {
        b.ToTable("RunHistories");
        b.HasKey(x => x.Id);

        b.Property(x => x.RequestedBy).IsRequired().HasMaxLength(64);
        b.Property(x => x.Status).IsRequired().HasMaxLength(24);

        b.HasIndex(x => new { x.SchoolYearId, x.RequestedAt });

        b.HasOne<SchoolYear>()
            .WithMany()
            .HasForeignKey(x => x.SchoolYearId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}