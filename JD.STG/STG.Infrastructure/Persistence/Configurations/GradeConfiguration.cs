using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> b)
    {
        b.ToTable("Grade");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(32).IsRequired();
        b.Property(x => x.Order).IsRequired();

        b.HasIndex(x => x.Name).IsUnique();
        b.HasIndex(x => x.Order).IsUnique();
    }
}