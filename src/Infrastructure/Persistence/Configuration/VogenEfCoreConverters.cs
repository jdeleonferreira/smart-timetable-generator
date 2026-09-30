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
using Vogen;

namespace SmartTimetableGenerator.Infrastructure.Persistence.Configuration;

[EfCoreConverter<InstitutionId>]
[EfCoreConverter<CampusId>]
[EfCoreConverter<ShiftId>]
[EfCoreConverter<BellScheduleId>]
[EfCoreConverter<DayTypeId>]
[EfCoreConverter<GradeId>]
[EfCoreConverter<AreaId>]
[EfCoreConverter<SubjectId>]
[EfCoreConverter<AcademicYearId>]
[EfCoreConverter<AcademicPeriodId>]
[EfCoreConverter<CalendarEntryId>]
[EfCoreConverter<StudyPlanId>]
[EfCoreConverter<StudyPlanItemId>]
[EfCoreConverter<SpaceId>]
[EfCoreConverter<TeacherId>]
[EfCoreConverter<CourseId>]
[EfCoreConverter<TeachingAssignmentId>]
[EfCoreConverter<TimetableId>]
[EfCoreConverter<LessonId>]
[EfCoreConverter<GenerationJobId>]
[EfCoreConverter<TrainingProjectId>]
[EfCoreConverter<ProjectActivityId>]
internal sealed partial class VogenEfCoreConverters;
