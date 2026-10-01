using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.DayTypes;

namespace SmartTimetableGenerator.Domain.AcademicYears;

[ValueObject<Guid>]
public readonly partial struct AcademicYearId;

public enum AcademicYearStatus
{
    Planning = 0,
    Active = 1,
    Closed = 2
}

/// <summary>
/// Año lectivo: fechas, periodos académicos y calendario (festivos, recesos, días con jornada especial).
/// </summary>
public class AcademicYear : AggregateRoot<AcademicYearId>
{
    public const int NameMaxLength = 50;

    private readonly List<AcademicPeriod> _periods = [];
    private readonly List<CalendarEntry> _calendarEntries = [];

    public int Year { get; private set; }
    public string Name { get; private set; } = null!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public AcademicYearStatus Status { get; private set; }

    public IReadOnlyList<AcademicPeriod> Periods => _periods.OrderBy(p => p.Number).ToList().AsReadOnly();
    public IReadOnlyList<CalendarEntry> CalendarEntries => _calendarEntries.OrderBy(e => e.Date).ToList().AsReadOnly();

    private AcademicYear() { } // Needed for EF Core

    public static AcademicYear Create(int year, DateOnly startDate, DateOnly endDate, string? name = null)
    {
        ThrowIfLessThan(year, 2000, nameof(year));
        ThrowIfGreaterThan(year, 2200, nameof(year));
        if (endDate <= startDate)
            throw new ArgumentOutOfRangeException(nameof(endDate), "La fecha final debe ser posterior a la inicial");

        return new AcademicYear
        {
            Id = AcademicYearId.From(Guid.CreateVersion7()),
            Year = year,
            Name = TextRules.Required(name ?? $"Año lectivo {year}", NameMaxLength, nameof(name)),
            StartDate = startDate,
            EndDate = endDate,
            Status = AcademicYearStatus.Planning
        };
    }

    public ErrorOr<Success> UpdateDates(DateOnly startDate, DateOnly endDate)
    {
        if (Status == AcademicYearStatus.Closed)
            return AcademicYearErrors.Closed;

        if (endDate <= startDate)
            return AcademicYearErrors.InvalidDateRange;

        if (_periods.Any(p => p.StartDate < startDate || p.EndDate > endDate))
            return AcademicYearErrors.PeriodOutsideYear;

        StartDate = startDate;
        EndDate = endDate;
        return Result.Success;
    }

    public ErrorOr<Success> Activate()
    {
        if (Status == AcademicYearStatus.Closed)
            return AcademicYearErrors.Closed;

        if (_periods.Count == 0)
            return AcademicYearErrors.NoPeriods;

        Status = AcademicYearStatus.Active;
        return Result.Success;
    }

    public ErrorOr<Success> Close()
    {
        if (Status != AcademicYearStatus.Active)
            return AcademicYearErrors.NotActive;

        Status = AcademicYearStatus.Closed;
        return Result.Success;
    }

    #region Periodos académicos

    public AcademicPeriod? FindPeriod(AcademicPeriodId periodId) => _periods.FirstOrDefault(p => p.Id == periodId);

    public AcademicPeriod? PeriodFor(DateOnly date) =>
        _periods.FirstOrDefault(p => p.StartDate <= date && date <= p.EndDate);

    public ErrorOr<AcademicPeriod> AddPeriod(string name, DateOnly startDate, DateOnly endDate)
    {
        var validation = ValidatePeriod(null, startDate, endDate);
        if (validation.IsError)
            return validation.Errors;

        var period = AcademicPeriod.Create(name, startDate, endDate);
        _periods.Add(period);
        RenumberPeriods();
        return period;
    }

    public ErrorOr<Success> UpdatePeriod(AcademicPeriodId periodId, string name, DateOnly startDate, DateOnly endDate)
    {
        var period = FindPeriod(periodId);
        if (period is null)
            return AcademicYearErrors.PeriodNotFound;

        var validation = ValidatePeriod(periodId, startDate, endDate);
        if (validation.IsError)
            return validation.Errors;

        period.Update(name, startDate, endDate);
        RenumberPeriods();
        return Result.Success;
    }

    /// <summary>
    /// La capa de aplicación debe verificar antes que ningún plan u horario use el periodo.
    /// </summary>
    public ErrorOr<Success> RemovePeriod(AcademicPeriodId periodId)
    {
        if (Status == AcademicYearStatus.Closed)
            return AcademicYearErrors.Closed;

        var period = FindPeriod(periodId);
        if (period is null)
            return AcademicYearErrors.PeriodNotFound;

        _periods.Remove(period);
        RenumberPeriods();
        return Result.Success;
    }

    private ErrorOr<Success> ValidatePeriod(AcademicPeriodId? excludeId, DateOnly startDate, DateOnly endDate)
    {
        if (Status == AcademicYearStatus.Closed)
            return AcademicYearErrors.Closed;

        if (endDate < startDate)
            return AcademicYearErrors.InvalidDateRange;

        if (startDate < StartDate || endDate > EndDate)
            return AcademicYearErrors.PeriodOutsideYear;

        var overlaps = _periods.Any(p => p.Id != excludeId && p.StartDate <= endDate && startDate <= p.EndDate);
        return overlaps ? AcademicYearErrors.PeriodsOverlap : Result.Success;
    }

    private void RenumberPeriods()
    {
        var number = 1;
        foreach (var period in _periods.OrderBy(p => p.StartDate))
            period.SetNumber(number++);
    }

    #endregion

    #region Calendario

    /// <summary>
    /// Carga los festivos de Colombia que caen dentro del año lectivo. No duplica los ya cargados.
    /// </summary>
    /// <returns>Cantidad de festivos agregados.</returns>
    public int LoadColombianHolidays()
    {
        var added = 0;
        for (var year = StartDate.Year; year <= EndDate.Year; year++)
        {
            foreach (var (date, name) in ColombianHolidays.ForYear(year))
            {
                if (date < StartDate || date > EndDate)
                    continue;

                if (_calendarEntries.Any(e => e.Date == date && e.Kind == CalendarEntryKind.Holiday))
                    continue;

                _calendarEntries.Add(CalendarEntry.Create(date, CalendarEntryKind.Holiday, name, campusId: null, dayTypeId: null, isAutomatic: true));
                added++;
            }
        }

        return added;
    }

    /// <summary>
    /// Registra días sin clase en un rango (receso, jornada pedagógica, etc.). Opcionalmente solo para una sede.
    /// </summary>
    public ErrorOr<Success> AddNonSchoolDays(DateOnly from, DateOnly to, CalendarEntryKind kind, string description, CampusId? campusId = null)
    {
        if (Status == AcademicYearStatus.Closed)
            return AcademicYearErrors.Closed;

        if (kind == CalendarEntryKind.SpecialSchedule)
            return AcademicYearErrors.InvalidEntryKind;

        if (to < from)
            return AcademicYearErrors.InvalidDateRange;

        if (from < StartDate || to > EndDate)
            return AcademicYearErrors.DateOutsideYear;

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            if (_calendarEntries.Any(e => e.Date == date && e.Kind == kind && e.CampusId == campusId))
                continue;

            _calendarEntries.Add(CalendarEntry.Create(date, kind, description, campusId, dayTypeId: null, isAutomatic: false));
        }

        return Result.Success;
    }

    /// <summary>
    /// Marca una fecha con un tipo de jornada especial (ej.: horario B). Reemplaza la marca previa del mismo alcance.
    /// </summary>
    public ErrorOr<Success> SetSpecialSchedule(DateOnly date, DayTypeId dayTypeId, string description, CampusId? campusId = null)
    {
        if (Status == AcademicYearStatus.Closed)
            return AcademicYearErrors.Closed;

        if (date < StartDate || date > EndDate)
            return AcademicYearErrors.DateOutsideYear;

        _calendarEntries.RemoveAll(e => e.Date == date && e.Kind == CalendarEntryKind.SpecialSchedule && e.CampusId == campusId);
        _calendarEntries.Add(CalendarEntry.Create(date, CalendarEntryKind.SpecialSchedule, description, campusId, dayTypeId, isAutomatic: false));
        return Result.Success;
    }

    public ErrorOr<Success> RemoveCalendarEntry(CalendarEntryId entryId)
    {
        if (Status == AcademicYearStatus.Closed)
            return AcademicYearErrors.Closed;

        var entry = _calendarEntries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null)
            return AcademicYearErrors.CalendarEntryNotFound;

        _calendarEntries.Remove(entry);
        return Result.Success;
    }

    /// <summary>
    /// ¿Hay clase en esta fecha para la sede y los días de la jornada indicados?
    /// </summary>
    public bool IsSchoolDay(DateOnly date, CampusId campusId, SchoolDays shiftDays)
    {
        if (date < StartDate || date > EndDate)
            return false;

        if (!shiftDays.Includes(date.DayOfWeek))
            return false;

        return !_calendarEntries.Any(e =>
            e.Date == date &&
            e.Kind != CalendarEntryKind.SpecialSchedule &&
            (e.CampusId is null || e.CampusId == campusId));
    }

    /// <summary>
    /// Tipo de jornada especial asignado a la fecha (la marca de la sede tiene prioridad sobre la institucional).
    /// Null significa que aplica el tipo por defecto.
    /// </summary>
    public DayTypeId? SpecialDayTypeFor(DateOnly date, CampusId campusId)
    {
        var entries = _calendarEntries
            .Where(e => e.Date == date && e.Kind == CalendarEntryKind.SpecialSchedule)
            .ToList();

        return (entries.FirstOrDefault(e => e.CampusId == campusId) ?? entries.FirstOrDefault(e => e.CampusId is null))?.DayTypeId;
    }

    #endregion
}
