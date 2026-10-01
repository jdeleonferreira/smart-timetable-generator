using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.Spaces;

namespace SmartTimetableGenerator.Domain.StudyPlans;

[ValueObject<Guid>]
public readonly partial struct StudyPlanId;

public enum StudyPlanStatus
{
    Draft = 0,
    Approved = 1
}

/// <summary>
/// Plan de estudios de una sede para un año lectivo: qué asignaturas ve cada grado, con qué intensidad horaria (IH)
/// y cómo se dictan. Es la fuente de la que se generan los horarios. Todos los cursos de un grado en la sede lo comparten.
/// </summary>
public class StudyPlan : AggregateRoot<StudyPlanId>
{
    public const int NameMaxLength = 150;
    public const int NotesMaxLength = 4000;

    private readonly List<StudyPlanItem> _items = [];

    public AcademicYearId AcademicYearId { get; private set; }
    public CampusId CampusId { get; private set; }
    public string Name { get; private set; } = null!;
    public StudyPlanStatus Status { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }

    /// <summary>Notas generales que se imprimen al pie del documento del plan.</summary>
    public string? Notes { get; private set; }

    public IReadOnlyList<StudyPlanItem> Items => _items.AsReadOnly();

    private StudyPlan() { } // Needed for EF Core

    public static StudyPlan Create(AcademicYearId academicYearId, CampusId campusId, string name)
    {
        return new StudyPlan
        {
            Id = StudyPlanId.From(Guid.CreateVersion7()),
            AcademicYearId = academicYearId,
            CampusId = campusId,
            Name = TextRules.Required(name, NameMaxLength, nameof(name)),
            Status = StudyPlanStatus.Draft
        };
    }

    /// <summary>
    /// Crea un plan nuevo copiando las asignaturas, IH y reglas de otro plan (normalmente el del año anterior).
    /// Los ajustes de IH por periodo solo se conservan si el año lectivo es el mismo, porque los periodos cambian de un año a otro.
    /// </summary>
    public static StudyPlan CreateCopy(StudyPlan source, AcademicYearId academicYearId, CampusId campusId, string name)
    {
        ThrowIfNull(source);

        var plan = Create(academicYearId, campusId, name);
        plan.Notes = source.Notes;

        var keepPeriodHours = source.AcademicYearId == academicYearId;
        foreach (var item in source._items)
            plan._items.Add(item.Copy(keepPeriodHours));

        return plan;
    }

    public ErrorOr<Success> Update(string name, string? notes)
    {
        if (Status == StudyPlanStatus.Approved)
            return StudyPlanErrors.NotEditable;

        Name = TextRules.Required(name, NameMaxLength, nameof(name));
        Notes = TextRules.Optional(notes, NotesMaxLength, nameof(notes));
        return Result.Success;
    }

    public ErrorOr<Success> Approve(DateTimeOffset now)
    {
        if (Status == StudyPlanStatus.Approved)
            return StudyPlanErrors.AlreadyApproved;

        if (_items.Count == 0)
            return StudyPlanErrors.Empty;

        Status = StudyPlanStatus.Approved;
        ApprovedAt = now;
        return Result.Success;
    }

    public void Reopen()
    {
        Status = StudyPlanStatus.Draft;
        ApprovedAt = null;
    }

    #region Ítems (asignatura × grado)

    public StudyPlanItem? FindItem(StudyPlanItemId itemId) => _items.FirstOrDefault(i => i.Id == itemId);

    public StudyPlanItem? FindItem(GradeId gradeId, SubjectId subjectId) =>
        _items.FirstOrDefault(i => i.GradeId == gradeId && i.SubjectId == subjectId);

    public IReadOnlyList<StudyPlanItem> ItemsForGrade(GradeId gradeId) =>
        _items.Where(i => i.GradeId == gradeId).ToList();

    public ErrorOr<StudyPlanItem> AddItem(
        GradeId gradeId,
        SubjectId subjectId,
        DeliveryMode deliveryMode,
        int weeklyHours,
        ShiftId? targetShiftId = null,
        SubjectId? integratedIntoSubjectId = null,
        string? note = null)
    {
        if (Status == StudyPlanStatus.Approved)
            return StudyPlanErrors.NotEditable;

        if (FindItem(gradeId, subjectId) is not null)
            return StudyPlanErrors.DuplicateItem;

        var item = StudyPlanItem.Create(gradeId, subjectId, deliveryMode, weeklyHours, targetShiftId, integratedIntoSubjectId, note);
        if (item.IsError)
            return item.Errors;

        _items.Add(item.Value);
        return item.Value;
    }

    public ErrorOr<Success> UpdateItem(
        StudyPlanItemId itemId,
        DeliveryMode deliveryMode,
        int weeklyHours,
        ShiftId? targetShiftId = null,
        SubjectId? integratedIntoSubjectId = null,
        string? note = null)
    {
        if (Status == StudyPlanStatus.Approved)
            return StudyPlanErrors.NotEditable;

        var item = FindItem(itemId);
        if (item is null)
            return StudyPlanErrors.ItemNotFound;

        return item.Update(deliveryMode, weeklyHours, targetShiftId, integratedIntoSubjectId, note);
    }

    /// <summary>
    /// Define cómo se reparten las horas en la semana (bloques por día) y si la clase requiere un tipo de espacio.
    /// </summary>
    public ErrorOr<Success> SetItemDistribution(
        StudyPlanItemId itemId,
        int? maxHoursPerDay,
        int? maxConsecutiveHours,
        SpaceType? requiredSpaceType)
    {
        if (Status == StudyPlanStatus.Approved)
            return StudyPlanErrors.NotEditable;

        var item = FindItem(itemId);
        if (item is null)
            return StudyPlanErrors.ItemNotFound;

        return item.SetDistribution(maxHoursPerDay, maxConsecutiveHours, requiredSpaceType);
    }

    /// <summary>
    /// IH distinta en un periodo. <paramref name="weeklyHours"/> = null elimina el ajuste y vuelve a la IH general.
    /// </summary>
    public ErrorOr<Success> SetItemPeriodHours(StudyPlanItemId itemId, AcademicPeriodId periodId, int? weeklyHours)
    {
        if (Status == StudyPlanStatus.Approved)
            return StudyPlanErrors.NotEditable;

        var item = FindItem(itemId);
        if (item is null)
            return StudyPlanErrors.ItemNotFound;

        return item.SetPeriodHours(periodId, weeklyHours);
    }

    public ErrorOr<Success> RemoveItem(StudyPlanItemId itemId)
    {
        if (Status == StudyPlanStatus.Approved)
            return StudyPlanErrors.NotEditable;

        var item = FindItem(itemId);
        if (item is null)
            return StudyPlanErrors.ItemNotFound;

        _items.Remove(item);
        return Result.Success;
    }

    #endregion

    #region Totales

    /// <summary>
    /// Total semanal de horas de un grado en su jornada (solo asignaturas regulares; no incluye transversales ni contrajornada).
    /// </summary>
    public int WeeklyTotal(GradeId gradeId, AcademicPeriodId? periodId = null) =>
        _items
            .Where(i => i.GradeId == gradeId && i.DeliveryMode == DeliveryMode.Regular)
            .Sum(i => i.HoursFor(periodId));

    /// <summary>
    /// Total semanal de horas en contrajornada de un grado.
    /// </summary>
    public int WeeklyCounterShiftTotal(GradeId gradeId, AcademicPeriodId? periodId = null) =>
        _items
            .Where(i => i.GradeId == gradeId && i.DeliveryMode == DeliveryMode.CounterShift)
            .Sum(i => i.HoursFor(periodId));

    #endregion
}
