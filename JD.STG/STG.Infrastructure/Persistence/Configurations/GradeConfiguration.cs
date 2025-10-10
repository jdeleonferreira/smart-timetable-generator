// STG.Infrastructure/Persistence/Configurations/GradeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

public sealed class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> b)
    {
        b.ToTable("Grades");

        b.HasKey(x => x.Id);

        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Grade.MaxNameLength);

        b.Property(x => x.Order)
            .HasDefaultValue((byte)0);

        b.HasOne(x => x.SchoolYear)
            .WithMany() // o .WithMany(y => y.Grades) si lo tienes
            .HasForeignKey(x => x.SchoolYearId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.SchoolYearId, x.Name })
            .IsUnique();

        // Si usas RowVersion aquí:
        // b.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
