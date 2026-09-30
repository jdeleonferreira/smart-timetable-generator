using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTimetableGenerator.Infrastructure.Identity;

namespace SmartTimetableGenerator.Infrastructure.Persistence.Configuration;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();

        // Un docente tiene a lo sumo un usuario
        builder.HasIndex(x => x.TeacherId).IsUnique();
    }
}
