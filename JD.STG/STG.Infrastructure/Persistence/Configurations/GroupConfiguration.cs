// Configurations/GroupConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> b)
    {
        b.ToTable("Group");
        b.HasKey(x => x.Id);

        b.Property(x => x.GradeId).IsRequired();
        b.Property(x => x.GradeName).HasMaxLength(32).IsRequired();
        b.Property(x => x.Label).HasMaxLength(8).IsRequired();
        b.Property(x => x.Size).IsRequired();

        b.HasIndex(x => new { x.GradeId, x.Label }).IsUnique();
        b.HasIndex(x => new { x.GradeName, x.Label });
    }
}
