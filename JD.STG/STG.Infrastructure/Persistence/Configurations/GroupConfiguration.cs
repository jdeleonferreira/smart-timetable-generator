using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> b)
    {
        b.ToTable("Groups");
        b.HasKey(x => x.Id);

        b.Property(x => x.GradeName)
            .IsRequired()
            .HasMaxLength(Group.MaxNameLength);

        b.HasIndex(x => new { x.GradeId, x.GradeName }).IsUnique();

        b.HasOne<Grade>()
            .WithMany()
            .HasForeignKey(x => x.GradeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
