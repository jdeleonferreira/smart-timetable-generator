// Configurations/SchoolYearConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;
using STG.Domain.ValueObjects;

public sealed class SchoolYearConfiguration : IEntityTypeConfiguration<SchoolYear>
{
    public void Configure(EntityTypeBuilder<SchoolYear> b)
    {
        b.ToTable("SchoolYear");
        b.HasKey(x => x.Id);
        b.Property(x => x.Year).IsRequired();

        // WeekConfig como owned type
        b.OwnsOne(x => x.Week, ow =>
        {
            ow.Property(p => p.BlocksPerDay).HasColumnName("BlocksPerDay").IsRequired();
            ow.Property(p => p.BlockLengthMinutes).HasColumnName("BlockLengthMinutes").IsRequired();

            // Days: almacénalo como CSV simple (MVP) o JSON. Aquí CSV.
            ow.Property<string>("DaysCsv")
              .HasColumnName("DaysCsv")
              .IsRequired();

            ow.Ignore(p => p.Days); // mapeo manual via backing field si luego lo necesitas
        });

        b.Property(x => x.IsOpen).HasDefaultValue(true);
        b.HasIndex(x => x.Year).IsUnique();
    }
}
