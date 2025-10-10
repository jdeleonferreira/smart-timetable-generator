// FILE: STG.Infrastructure/Persistence/StgDbContext.cs
using Microsoft.EntityFrameworkCore;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence;

public sealed class StgDbContext : DbContext
{
    public StgDbContext(DbContextOptions<StgDbContext> options) : base(options) { }

    public DbSet<SchoolYear> SchoolYears => Set<SchoolYear>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<PeriodSlot> PeriodSlots => Set<PeriodSlot>();
    public DbSet<SchedulingConfig> SchedulingConfigs => Set<SchedulingConfig>();
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<StudyPlanEntry> StudyPlanEntries => Set<StudyPlanEntry>();
    public DbSet<SubjectWeightProfile> SubjectWeightProfiles => Set<SubjectWeightProfile>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Timetable> Timetables => Set<Timetable>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();
    public DbSet<AvailabilityBlock> AvailabilityBlocks => Set<AvailabilityBlock>();
    public DbSet<RunHistory> RunHistories => Set<RunHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StgDbContext).Assembly);
    }
}
