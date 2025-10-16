using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class SchoolYearConfiguration : IEntityTypeConfiguration<SchoolYear>
{
    public void Configure(EntityTypeBuilder<SchoolYear> b)
    {
        b.ToTable("SchoolYears");
        b.HasKey(x => x.Id);
        b.Property(x => x.Year).IsRequired();
        b.HasIndex(x => x.Year).IsUnique();
    }
}
