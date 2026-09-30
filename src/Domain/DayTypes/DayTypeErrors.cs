namespace SmartTimetableGenerator.Domain.DayTypes;

public static class DayTypeErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "DayType.NotFound",
        "El tipo de jornada no existe");

    public static readonly Error DuplicateCode = Error.Conflict(
        "DayType.DuplicateCode",
        "Ya existe un tipo de jornada con ese código");
}
