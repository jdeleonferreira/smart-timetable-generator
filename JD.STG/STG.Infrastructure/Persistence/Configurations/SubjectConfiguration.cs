using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name)
               .IsRequired()
               .HasMaxLength(100);
        builder.Property(s => s.NeedsLab)
               .IsRequired();
        builder.Property(s => s.MustBeDouble)
               .IsRequired();
    }
}
