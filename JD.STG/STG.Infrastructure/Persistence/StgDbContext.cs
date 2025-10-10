// StgDbContext.cs
using Microsoft.EntityFrameworkCore;
using STG.Domain.Entities;

public sealed class StgDbContext : DbContext
{
    public StgDbContext(DbContextOptions<StgDbContext> options) : base(options) { }

    public DbSet<SchoolYear> SchoolYears => Set<SchoolYear>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Timetable> Timetables => Set<Timetable>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();
    public DbSet<StudyPlanEntry> StudyPlanEntries => Set<StudyPlanEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StgDbContext).Assembly);
    }
}
