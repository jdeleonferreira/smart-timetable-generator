using Microsoft.EntityFrameworkCore;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence;

public sealed class StgDbContext : DbContext
{
    public StgDbContext(DbContextOptions<StgDbContext> options) : base(options) { }

    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AvailabilityBlock> AvailabilityBlocks => Set<AvailabilityBlock>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<PeriodSlot> PeriodSlots => Set<PeriodSlot>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RunHistory> RunHistories => Set<RunHistory>();
    public DbSet<SchedulingConfig> SchedulingConfigs => Set<SchedulingConfig>();
    public DbSet<SchoolYear> SchoolYears => Set<SchoolYear>();
    public DbSet<StudyArea> StudyAreas => Set<StudyArea>();
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<StudyPlanEntry> StudyPlanEntries => Set<StudyPlanEntry>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<SubjectWeightProfile> SubjectWeightProfiles => Set<SubjectWeightProfile>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Timetable> Timetables => Set<Timetable>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StgDbContext).Assembly);
    }
}
