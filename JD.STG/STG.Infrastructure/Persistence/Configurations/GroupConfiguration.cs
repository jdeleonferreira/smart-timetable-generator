using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;


namespace STG.Infrastructure.Persistence.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Grade)
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(g => g.Label)
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(g => g.Size)
               .IsRequired();
    }
}
