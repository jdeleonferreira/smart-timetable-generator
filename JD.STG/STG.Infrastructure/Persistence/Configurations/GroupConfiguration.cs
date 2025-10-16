using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> b)
    {
        b.ToTable("Group");
        b.HasKey(x => x.Id);

        b.Property(x => x.GradeId).IsRequired();
        b.Property(x => x.Name).HasMaxLength(16).IsRequired();

        // Unique per grade (GradeId, Name)
        b.HasIndex(x => new { x.GradeId, x.Name }).IsUnique();

        b.HasOne(x => x.Grade)
         .WithMany() // groups are catalog-like; adjust if you add Grade.Groups
         .HasForeignKey(x => x.GradeId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
