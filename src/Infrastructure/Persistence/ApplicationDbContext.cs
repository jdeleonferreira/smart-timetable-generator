using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Common.Interfaces;
using SmartTimetableGenerator.Domain.Courses;
using SmartTimetableGenerator.Domain.DayTypes;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.Institutions;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.TeachingAssignments;
using SmartTimetableGenerator.Domain.TimetableGeneration;
using SmartTimetableGenerator.Domain.Timetables;
using SmartTimetableGenerator.Domain.TrainingProjects;
using SmartTimetableGenerator.Infrastructure.Identity;
using SmartTimetableGenerator.Infrastructure.Persistence.Configuration;
using System.Reflection;

namespace SmartTimetableGenerator.Infrastructure.Persistence;

/// <summary>
/// Base de datos de la aplicación: agregados del dominio y cuentas de usuario (ASP.NET Identity).
/// </summary>
public class ApplicationDbContext(DbContextOptions options)
    : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
{
    public DbSet<Institution> Institutions => AggregateRootSet<Institution>();
    public DbSet<Campus> Campuses => AggregateRootSet<Campus>();
    public DbSet<DayType> DayTypes => AggregateRootSet<DayType>();
    public DbSet<Grade> Grades => AggregateRootSet<Grade>();
    public DbSet<Area> Areas => AggregateRootSet<Area>();
    public DbSet<AcademicYear> AcademicYears => AggregateRootSet<AcademicYear>();
    public DbSet<StudyPlan> StudyPlans => AggregateRootSet<StudyPlan>();
    public DbSet<Space> Spaces => AggregateRootSet<Space>();
    public DbSet<Teacher> Teachers => AggregateRootSet<Teacher>();
    public DbSet<Course> Courses => AggregateRootSet<Course>();
    public DbSet<TeachingAssignment> TeachingAssignments => AggregateRootSet<TeachingAssignment>();
    public DbSet<Timetable> Timetables => AggregateRootSet<Timetable>();
    public DbSet<GenerationJob> GenerationJobs => AggregateRootSet<GenerationJob>();
    public DbSet<TrainingProject> TrainingProjects => AggregateRootSet<TrainingProject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Primero Identity, luego las configuraciones propias (incluida la de ApplicationUser)
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.RegisterAllInVogenEfCoreConverters();

        // Enums legibles en la base de datos
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(40);
    }

    private DbSet<T> AggregateRootSet<T>() where T : class, IAggregateRoot => Set<T>();
}
