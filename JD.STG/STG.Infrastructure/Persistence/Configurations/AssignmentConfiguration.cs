using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(a => a.Id); // PK simple

        builder.Property(a => a.GroupCode).IsRequired().HasMaxLength(10);
        builder.Property(a => a.Subject).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Teacher).IsRequired().HasMaxLength(120);
        builder.Property(a => a.Room).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Blocks).IsRequired();

        var slot = builder.OwnsOne(a => a.Slot);

        slot.Property(s => s.Day)
            .HasConversion<int>()
            .HasColumnName("Slot_Day")
            .IsRequired();

        slot.Property(s => s.Block)
            .HasColumnName("Slot_Block")
            .IsRequired();

        // Índice SOLO dentro del owned type (opcional)
        slot.HasIndex(s => new { s.Day, s.Block })
            .HasDatabaseName("IX_Assignments_Slot");
    }
}

