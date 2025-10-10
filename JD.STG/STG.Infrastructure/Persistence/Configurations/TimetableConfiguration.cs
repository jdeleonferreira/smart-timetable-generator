using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class TimetableConfiguration : IEntityTypeConfiguration<Timetable>
{
    public void Configure(EntityTypeBuilder<Timetable> b)
    {
        b.ToTable("Timetables");
        b.HasKey(x => x.Id);

        b.HasIndex(x => new { x.SchoolYearId, x.GroupId }).IsUnique();

        b.HasOne<SchoolYear>().WithMany().HasForeignKey(x => x.SchoolYearId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Group>().WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
    }
}
