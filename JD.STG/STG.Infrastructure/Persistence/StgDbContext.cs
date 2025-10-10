using Microsoft.EntityFrameworkCore;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence;

public class StgDbContext : DbContext
{
    public StgDbContext(DbContextOptions<StgDbContext> options) : base(options) { }

    // DbSets
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<StudyPlanEntry> CurriculumLines => Set<StudyPlanEntry>();
    public DbSet<Timetable> Timetables => Set<Timetable>();
    public DbSet<Assignment> Assignments => Set<Assignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StgDbContext).Assembly);
    }
}
