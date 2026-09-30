namespace SmartTimetableGenerator.Domain.Campuses;

public static class CampusErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Campus.NotFound",
        "La sede no existe");

    public static readonly Error ShiftNotFound = Error.NotFound(
        "Campus.ShiftNotFound",
        "La jornada no existe en esta sede");

    public static readonly Error DuplicateShiftName = Error.Conflict(
        "Campus.DuplicateShiftName",
        "Ya existe una jornada con ese nombre en la sede");

    public static readonly Error ShiftWithoutDays = Error.Validation(
        "Campus.ShiftWithoutDays",
        "La jornada debe tener al menos un día de clase");

    public static readonly Error BellScheduleWithoutClasses = Error.Validation(
        "Campus.BellScheduleWithoutClasses",
        "El horario de timbre debe tener al menos una franja de clase");

    public static readonly Error BellBlocksOverlap = Error.Validation(
        "Campus.BellBlocksOverlap",
        "Las franjas del horario de timbre se superponen");
}
