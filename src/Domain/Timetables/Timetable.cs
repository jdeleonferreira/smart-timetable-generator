using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Courses;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.Teachers;

namespace SmartTimetableGenerator.Domain.Timetables;

[ValueObject<Guid>]
public readonly partial struct TimetableId;

public enum TimetableStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

/// <summary>
/// Horario semanal de una sede para un periodo académico, generado a partir del plan de estudios.
/// Cada clase se ubica en (jornada, día, número de franja). Las horas reales dependen del tipo de jornada
/// del día (A, B…), así un mismo horario sirve para todos los tipos.
/// </summary>
/// <remarks>
/// El agregado impide cruces dentro de la misma jornada (curso, docente o espacio en la misma franja).
/// Los cruces de un docente entre sedes o jornadas distintas se validan por horas en el generador.
/// </remarks>
public class Timetable : AggregateRoot<TimetableId>
{
    public const int NameMaxLength = 150;

    private readonly List<Lesson> _lessons = [];

    public AcademicYearId AcademicYearId { get; private set; }
    public CampusId CampusId { get; private set; }
    public AcademicPeriodId AcademicPeriodId { get; private set; }
    public StudyPlanId StudyPlanId { get; private set; }
    public string Name { get; private set; } = null!;
    public TimetableStatus Status { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    public IReadOnlyList<Lesson> Lessons => _lessons.AsReadOnly();

    private Timetable() { } // Needed for EF Core

    public static Timetable Create(
        AcademicYearId academicYearId,
        CampusId campusId,
        AcademicPeriodId academicPeriodId,
        StudyPlanId studyPlanId,
        string name)
    {
        return new Timetable
        {
            Id = TimetableId.From(Guid.CreateVersion7()),
            AcademicYearId = academicYearId,
            CampusId = campusId,
            AcademicPeriodId = academicPeriodId,
            StudyPlanId = studyPlanId,
            Name = TextRules.Required(name, NameMaxLength, nameof(name)),
            Status = TimetableStatus.Draft
        };
    }

    /// <summary>
    /// Copia un horario para otro periodo del mismo año (punto de partida cuando el horario cambia entre periodos).
    /// </summary>
    public static Timetable CreateCopy(Timetable source, AcademicPeriodId academicPeriodId, string name)
    {
        ThrowIfNull(source);

        var copy = Create(source.AcademicYearId, source.CampusId, academicPeriodId, source.StudyPlanId, name);
        foreach (var lesson in source._lessons)
            copy._lessons.Add(Lesson.Create(lesson.Placement, lesson.IsLocked));

        return copy;
    }

    public ErrorOr<Success> Rename(string name)
    {
        if (Status == TimetableStatus.Archived)
            return TimetableErrors.NotEditable;

        Name = TextRules.Required(name, NameMaxLength, nameof(name));
        return Result.Success;
    }

    public ErrorOr<Success> Publish(DateTimeOffset now)
    {
        if (Status != TimetableStatus.Draft)
            return TimetableErrors.NotEditable;

        if (_lessons.Count == 0)
            return TimetableErrors.Empty;

        Status = TimetableStatus.Published;
        PublishedAt = now;
        return Result.Success;
    }

    /// <summary>Vuelve a borrador para editarlo.</summary>
    public ErrorOr<Success> Unpublish()
    {
        if (Status != TimetableStatus.Published)
            return TimetableErrors.NotPublished;

        Status = TimetableStatus.Draft;
        PublishedAt = null;
        return Result.Success;
    }

    public void Archive() => Status = TimetableStatus.Archived;

    #region Clases

    public Lesson? FindLesson(LessonId lessonId) => _lessons.FirstOrDefault(l => l.Id == lessonId);

    public IReadOnlyList<Lesson> LessonsForCourse(CourseId courseId) =>
        _lessons.Where(l => l.CourseId == courseId).OrderBy(l => l.Day).ThenBy(l => l.PeriodNumber).ToList();

    public IReadOnlyList<Lesson> LessonsForTeacher(TeacherId teacherId) =>
        _lessons.Where(l => l.TeacherId == teacherId).OrderBy(l => l.Day).ThenBy(l => l.PeriodNumber).ToList();

    public ErrorOr<Lesson> AddLesson(LessonPlacement placement, bool isLocked = false)
    {
        if (Status != TimetableStatus.Draft)
            return TimetableErrors.NotEditable;

        var conflict = FindConflict(placement, excludeLessonId: null);
        if (conflict is not null)
            return conflict.Value;

        var lesson = Lesson.Create(placement, isLocked);
        _lessons.Add(lesson);
        return lesson;
    }

    /// <summary>
    /// Mueve una clase a otra franja y, opcionalmente, a otro espacio.
    /// </summary>
    public ErrorOr<Success> MoveLesson(LessonId lessonId, DayOfWeek day, int periodNumber, SpaceId? spaceId)
    {
        if (Status != TimetableStatus.Draft)
            return TimetableErrors.NotEditable;

        var lesson = FindLesson(lessonId);
        if (lesson is null)
            return TimetableErrors.LessonNotFound;

        var target = lesson.Placement with { Day = day, PeriodNumber = periodNumber, SpaceId = spaceId };
        var conflict = FindConflict(target, lessonId);
        if (conflict is not null)
            return conflict.Value;

        lesson.MoveTo(target);
        return Result.Success;
    }

    public ErrorOr<Success> ChangeLessonTeacher(LessonId lessonId, TeacherId? teacherId)
    {
        if (Status != TimetableStatus.Draft)
            return TimetableErrors.NotEditable;

        var lesson = FindLesson(lessonId);
        if (lesson is null)
            return TimetableErrors.LessonNotFound;

        var target = lesson.Placement with { TeacherId = teacherId };
        var conflict = FindConflict(target, lessonId);
        if (conflict is not null)
            return conflict.Value;

        lesson.MoveTo(target);
        return Result.Success;
    }

    public ErrorOr<Success> SetLessonLocked(LessonId lessonId, bool isLocked)
    {
        if (Status != TimetableStatus.Draft)
            return TimetableErrors.NotEditable;

        var lesson = FindLesson(lessonId);
        if (lesson is null)
            return TimetableErrors.LessonNotFound;

        lesson.SetLocked(isLocked);
        return Result.Success;
    }

    public ErrorOr<Success> RemoveLesson(LessonId lessonId)
    {
        if (Status != TimetableStatus.Draft)
            return TimetableErrors.NotEditable;

        var lesson = FindLesson(lessonId);
        if (lesson is null)
            return TimetableErrors.LessonNotFound;

        _lessons.Remove(lesson);
        return Result.Success;
    }

    /// <summary>
    /// Resultado del generador: reemplaza todas las clases no fijadas por las nuevas ubicaciones.
    /// Las clases fijadas (<see cref="Lesson.IsLocked"/>) se conservan.
    /// </summary>
    public ErrorOr<Success> ReplaceUnlockedLessons(IEnumerable<LessonPlacement> placements)
    {
        ThrowIfNull(placements);

        if (Status != TimetableStatus.Draft)
            return TimetableErrors.NotEditable;

        var locked = _lessons.Where(l => l.IsLocked).ToList();
        var previous = _lessons.ToList();

        _lessons.Clear();
        _lessons.AddRange(locked);

        foreach (var placement in placements)
        {
            var conflict = FindConflict(placement, excludeLessonId: null);
            if (conflict is not null)
            {
                _lessons.Clear();
                _lessons.AddRange(previous);
                return conflict.Value;
            }

            _lessons.Add(Lesson.Create(placement, isLocked: false));
        }

        return Result.Success;
    }

    private Error? FindConflict(LessonPlacement placement, LessonId? excludeLessonId)
    {
        var sameSlot = _lessons
            .Where(l => l.Id != excludeLessonId &&
                        l.ShiftId == placement.ShiftId &&
                        l.Day == placement.Day &&
                        l.PeriodNumber == placement.PeriodNumber)
            .ToList();

        if (sameSlot.Any(l => l.CourseId == placement.CourseId))
            return TimetableErrors.CourseBusy;

        if (placement.TeacherId is not null && sameSlot.Any(l => l.TeacherId == placement.TeacherId))
            return TimetableErrors.TeacherBusy;

        if (placement.SpaceId is not null && sameSlot.Any(l => l.SpaceId == placement.SpaceId))
            return TimetableErrors.SpaceBusy;

        return null;
    }

    #endregion
}
