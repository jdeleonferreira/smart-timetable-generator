// Configurations/SubjectConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

public sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.ToTable("Subject");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.RequiresLab).IsRequired();
        b.Property(x => x.RequiresDoublePeriod).IsRequired();

        b.HasIndex(x => x.Name).IsUnique();
    }
}