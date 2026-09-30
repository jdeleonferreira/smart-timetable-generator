namespace SmartTimetableGenerator.Domain.AcademicYears;

public static class AcademicYearErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "AcademicYear.NotFound",
        "El año lectivo no existe");

    public static readonly Error DuplicateYear = Error.Conflict(
        "AcademicYear.DuplicateYear",
        "Ya existe un año lectivo para ese año");

    public static readonly Error Closed = Error.Conflict(
        "AcademicYear.Closed",
        "El año lectivo está cerrado y no admite cambios");

    public static readonly Error NotActive = Error.Conflict(
        "AcademicYear.NotActive",
        "El año lectivo no está activo");

    public static readonly Error NoPeriods = Error.Validation(
        "AcademicYear.NoPeriods",
        "El año lectivo debe tener al menos un periodo académico");

    public static readonly Error InvalidDateRange = Error.Validation(
        "AcademicYear.InvalidDateRange",
        "La fecha final debe ser posterior a la inicial");

    public static readonly Error DateOutsideYear = Error.Validation(
        "AcademicYear.DateOutsideYear",
        "La fecha está fuera del año lectivo");

    public static readonly Error PeriodNotFound = Error.NotFound(
        "AcademicYear.PeriodNotFound",
        "El periodo académico no existe");

    public static readonly Error PeriodOutsideYear = Error.Validation(
        "AcademicYear.PeriodOutsideYear",
        "El periodo debe estar dentro de las fechas del año lectivo");

    public static readonly Error PeriodsOverlap = Error.Validation(
        "AcademicYear.PeriodsOverlap",
        "El periodo se superpone con otro periodo");

    public static readonly Error CalendarEntryNotFound = Error.NotFound(
        "AcademicYear.CalendarEntryNotFound",
        "La fecha del calendario no existe");

    public static readonly Error InvalidEntryKind = Error.Validation(
        "AcademicYear.InvalidEntryKind",
        "Use SetSpecialSchedule para los días con jornada especial");
}
