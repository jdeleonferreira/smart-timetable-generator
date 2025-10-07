using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public class TimetableConfiguration : IEntityTypeConfiguration<Timetable>
{
    public void Configure(EntityTypeBuilder<Timetable> builder)
    {
        builder.ToTable("Timetables");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Year).IsRequired();

        // WeekConfig no se persistirá como tabla; se serializa como JSON
        builder.OwnsOne(t => t.WeekConfig, wc =>
        {
            wc.ToJson();
        });

        builder.HasMany<Assignment>("_assignments")
               .WithOne()
               .OnDelete(DeleteBehavior.Cascade);
    }
}
