using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.Spaces;

namespace SmartTimetableGenerator.Domain.StudyPlans;

[ValueObject<Guid>]
public readonly partial struct StudyPlanItemId;

/// <summary>
/// Cómo se dicta una asignatura en un grado.
/// </summary>
public enum DeliveryMode
{
    /// <summary>Tiene IH y se programa en la jornada del curso.</summary>
    Regular = 0,

    /// <summary>No ocupa franjas; se trabaja dentro de otra asignatura. En el documento sale con «*».</summary>
    Transversal = 1,

    /// <summary>Tiene IH, pero se programa en otra jornada de la sede (ej.: en la tarde). En el documento sale con «*».</summary>
    CounterShift = 2
}

/// <summary>
/// Línea del plan de estudios: una asignatura en un grado, con su IH semanal y reglas de distribución.
/// </summary>
public class StudyPlanItem : Entity<StudyPlanItemId>
{
    public const int NoteMaxLength = 500;
    public const int MaxWeeklyHours = 40;

    private readonly List<PeriodHours> _periodHours = [];

    public GradeId GradeId { get; private set; }
    public SubjectId SubjectId { get; private set; }
    public DeliveryMode DeliveryMode { get; private set; }

    /// <summary>IH semanal general (horas de clase por semana).</summary>
    public int WeeklyHours { get; private set; }

    /// <summary>Jornada donde se programa cuando el tipo es contrajornada.</summary>
    public ShiftId? TargetShiftId { get; private set; }

    /// <summary>Asignatura en la que se integra cuando es transversal (ej.: Competencia ciudadana → Sociales).</summary>
    public SubjectId? IntegratedIntoSubjectId { get; private set; }

    /// <summary>Nota que acompaña el «*» en el documento.</summary>
    public string? Note { get; private set; }

    /// <summary>Máximo de horas de esta asignatura en un mismo día. Null = sin límite propio.</summary>
    public int? MaxHoursPerDay { get; private set; }

    /// <summary>Máximo de horas seguidas (2 = se permiten bloques dobles). Null = sin límite propio.</summary>
    public int? MaxConsecutiveHours { get; private set; }

    /// <summary>Tipo de espacio requerido (ej.: sala de informática). Null = salón del curso.</summary>
    public SpaceType? RequiredSpaceType { get; private set; }

    /// <summary>IH distinta por periodo académico.</summary>
    public IReadOnlyList<PeriodHours> PeriodHours => _periodHours.AsReadOnly();

    private StudyPlanItem() { } // Needed for EF Core

    internal static ErrorOr<StudyPlanItem> Create(
        GradeId gradeId,
        SubjectId subjectId,
        DeliveryMode deliveryMode,
        int weeklyHours,
        ShiftId? targetShiftId,
        SubjectId? integratedIntoSubjectId,
        string? note)
    {
        var item = new StudyPlanItem
        {
            Id = StudyPlanItemId.From(Guid.CreateVersion7()),
            GradeId = gradeId,
            SubjectId = subjectId
        };

        var result = item.Update(deliveryMode, weeklyHours, targetShiftId, integratedIntoSubjectId, note);
        return result.IsError ? result.Errors : item;
    }

    internal StudyPlanItem Copy(bool keepPeriodHours)
    {
        var copy = new StudyPlanItem
        {
            Id = StudyPlanItemId.From(Guid.CreateVersion7()),
            GradeId = GradeId,
            SubjectId = SubjectId,
            DeliveryMode = DeliveryMode,
            WeeklyHours = WeeklyHours,
            TargetShiftId = TargetShiftId,
            IntegratedIntoSubjectId = IntegratedIntoSubjectId,
            Note = Note,
            MaxHoursPerDay = MaxHoursPerDay,
            MaxConsecutiveHours = MaxConsecutiveHours,
            RequiredSpaceType = RequiredSpaceType
        };

        if (keepPeriodHours)
            copy._periodHours.AddRange(_periodHours);

        return copy;
    }

    internal ErrorOr<Success> Update(
        DeliveryMode deliveryMode,
        int weeklyHours,
        ShiftId? targetShiftId,
        SubjectId? integratedIntoSubjectId,
        string? note)
    {
        if (weeklyHours is < 0 or > MaxWeeklyHours)
            return StudyPlanErrors.InvalidWeeklyHours;

        switch (deliveryMode)
        {
            case DeliveryMode.Transversal:
                weeklyHours = 0;
                targetShiftId = null;
                _periodHours.Clear();
                break;

            case DeliveryMode.CounterShift:
                if (targetShiftId is null)
                    return StudyPlanErrors.CounterShiftRequiresShift;
                integratedIntoSubjectId = null;
                break;

            case DeliveryMode.Regular:
            default:
                targetShiftId = null;
                integratedIntoSubjectId = null;
                break;
        }

        if (integratedIntoSubjectId == SubjectId)
            return StudyPlanErrors.IntegratedIntoItself;

        DeliveryMode = deliveryMode;
        WeeklyHours = weeklyHours;
        TargetShiftId = targetShiftId;
        IntegratedIntoSubjectId = integratedIntoSubjectId;
        Note = TextRules.Optional(note, NoteMaxLength, nameof(note));
        return Result.Success;
    }

    internal ErrorOr<Success> SetDistribution(int? maxHoursPerDay, int? maxConsecutiveHours, SpaceType? requiredSpaceType)
    {
        if (maxHoursPerDay is < 1 || maxConsecutiveHours is < 1)
            return StudyPlanErrors.InvalidDistribution;

        if (maxHoursPerDay is not null && maxConsecutiveHours is not null && maxConsecutiveHours > maxHoursPerDay)
            return StudyPlanErrors.InvalidDistribution;

        MaxHoursPerDay = maxHoursPerDay;
        MaxConsecutiveHours = maxConsecutiveHours;
        RequiredSpaceType = requiredSpaceType;
        return Result.Success;
    }

    internal ErrorOr<Success> SetPeriodHours(AcademicPeriodId periodId, int? weeklyHours)
    {
        if (DeliveryMode == DeliveryMode.Transversal)
            return StudyPlanErrors.TransversalHasNoHours;

        _periodHours.RemoveAll(p => p.AcademicPeriodId == periodId);

        if (weeklyHours is null)
            return Result.Success;

        if (weeklyHours is < 0 or > MaxWeeklyHours)
            return StudyPlanErrors.InvalidWeeklyHours;

        _periodHours.Add(new PeriodHours(periodId, weeklyHours.Value));
        return Result.Success;
    }

    /// <summary>
    /// IH que aplica en el periodo (ajuste del periodo o, si no hay, la IH general).
    /// </summary>
    public int HoursFor(AcademicPeriodId? periodId)
    {
        if (DeliveryMode == DeliveryMode.Transversal)
            return 0;

        if (periodId is null)
            return WeeklyHours;

        var overrideHours = _periodHours.FirstOrDefault(p => p.AcademicPeriodId == periodId.Value);
        return overrideHours?.WeeklyHours ?? WeeklyHours;
    }
}
