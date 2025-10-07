using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("Teachers");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
               .IsRequired()
               .HasMaxLength(120);

        // Convertimos el HashSet de Subjects a JSON
        builder.Property(t => t.Subjects)
               .HasConversion(
                   v => string.Join(',', v),               // to DB
                   v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .ToHashSet(StringComparer.OrdinalIgnoreCase)
               );
    }
}
