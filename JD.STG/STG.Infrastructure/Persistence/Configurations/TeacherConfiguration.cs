using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> b)
    {
        b.ToTable("Teachers");
        b.HasKey(x => x.Id);

        b.Property(x => x.FullName).HasMaxLength(120).IsRequired();
        b.Property(x => x.Email).HasMaxLength(120);
        b.Property(x => x.MaxWeeklyLoad);
        b.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();

        b.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
    }
}
