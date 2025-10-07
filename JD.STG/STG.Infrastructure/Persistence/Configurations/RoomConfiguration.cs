using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(r => r.Capacity)
               .IsRequired();

        // Serializamos los tags como JSON
        builder.OwnsOne(r => r.Tags, tb =>
        {
            tb.ToJson();  // EF Core 8+ puede mapear a JSON en SQLite
        });
    }
}
