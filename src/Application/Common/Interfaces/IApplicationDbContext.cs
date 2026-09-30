using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
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

namespace SmartTimetableGenerator.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Institution> Institutions { get; }
    DbSet<Campus> Campuses { get; }
    DbSet<DayType> DayTypes { get; }
    DbSet<Grade> Grades { get; }
    DbSet<Area> Areas { get; }
    DbSet<AcademicYear> AcademicYears { get; }
    DbSet<StudyPlan> StudyPlans { get; }
    DbSet<Space> Spaces { get; }
    DbSet<Teacher> Teachers { get; }
    DbSet<Course> Courses { get; }
    DbSet<TeachingAssignment> TeachingAssignments { get; }
    DbSet<Timetable> Timetables { get; }
    DbSet<GenerationJob> GenerationJobs { get; }
    DbSet<TrainingProject> TrainingProjects { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
