using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.DayTypes;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.Institutions;

namespace SmartTimetableGenerator.Infrastructure.Persistence.Configuration;

public class InstitutionConfiguration : AuditableConfiguration<Institution>
{
    public override void PostConfigure(EntityTypeBuilder<Institution> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Institution.NameMaxLength).IsRequired();
        builder.Property(x => x.Nit).HasMaxLength(Institution.CodeMaxLength);
        builder.Property(x => x.DaneCode).HasMaxLength(Institution.CodeMaxLength);
        builder.Property(x => x.Address).HasMaxLength(Institution.AddressMaxLength);
    }
}

public class DayTypeConfiguration : AuditableConfiguration<DayType>
{
    public override void PostConfigure(EntityTypeBuilder<DayType> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(DayType.CodeMaxLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(DayType.NameMaxLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(DayType.DescriptionMaxLength);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class GradeConfiguration : AuditableConfiguration<Grade>
{
    public override void PostConfigure(EntityTypeBuilder<Grade> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Grade.NameMaxLength).IsRequired();
        builder.Property(x => x.ShortName).HasMaxLength(Grade.ShortNameMaxLength).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class AreaConfiguration : AuditableConfiguration<Area>
{
    public override void PostConfigure(EntityTypeBuilder<Area> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Area.NameMaxLength).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasMany(x => x.Subjects)
            .WithOne()
            .HasForeignKey("AreaId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SubjectConfiguration : AuditableConfiguration<Subject>
{
    public override void PostConfigure(EntityTypeBuilder<Subject> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(Subject.NameMaxLength).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(Subject.CodeMaxLength);
    }
}
