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

        builder.OwnsOne(t => t.WeekConfig);

        builder
            .HasMany(t => t.Assignments)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        // Indica el backing field y el modo de acceso
        var nav = builder.Metadata.FindNavigation(nameof(Timetable.Assignments));
        nav!.SetField("_assignments");
        nav.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
