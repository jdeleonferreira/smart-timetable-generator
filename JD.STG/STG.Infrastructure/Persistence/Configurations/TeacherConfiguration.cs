using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> b)
    {
        b.ToTable("Teachers");
        b.HasKey(x => x.Id);

        b.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(Teacher.MaxNameLength);

        b.Property(x => x.MaxDailyPeriods);
        b.Property(x => x.Tags).HasMaxLength(Teacher.MaxTagsLength);

        b.HasIndex(x => new { x.SchoolYearId, x.FullName });

        b.HasOne<SchoolYear>()
            .WithMany()
            .HasForeignKey(x => x.SchoolYearId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
