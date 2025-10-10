// Configurations/TeacherConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using STG.Domain.Entities;

public sealed class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> b)
    {
        b.ToTable("Teacher");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(128).IsRequired();

        // JSON converter para _subjects
        var converter = new ValueConverter<IReadOnlyCollection<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<IReadOnlyCollection<string>>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>());

        b.Property(typeof(IReadOnlyCollection<string>), "_subjects")
         .HasColumnName("SubjectsJson")
         .HasConversion(converter)
         .HasColumnType("nvarchar(max)")
         .IsRequired();

        b.Ignore(x => x.Subjects); // expuesto como IReadOnly desde la entidad
        b.HasIndex(x => x.Name).IsUnique(false);
    }
}
