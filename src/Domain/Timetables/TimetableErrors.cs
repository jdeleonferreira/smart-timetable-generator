namespace SmartTimetableGenerator.Domain.Timetables;

public static class TimetableErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Timetable.NotFound",
        "El horario no existe");

    public static readonly Error NotEditable = Error.Conflict(
        "Timetable.NotEditable",
        "Solo se puede modificar un horario en borrador");

    public static readonly Error NotPublished = Error.Conflict(
        "Timetable.NotPublished",
        "El horario no está publicado");

    public static readonly Error Empty = Error.Validation(
        "Timetable.Empty",
        "El horario no tiene clases");

    public static readonly Error LessonNotFound = Error.NotFound(
        "Timetable.LessonNotFound",
        "La clase no existe en el horario");

    public static readonly Error CourseBusy = Error.Conflict(
        "Timetable.CourseBusy",
        "El curso ya tiene clase en esa franja");

    public static readonly Error TeacherBusy = Error.Conflict(
        "Timetable.TeacherBusy",
        "El docente ya tiene clase en esa franja");

    public static readonly Error SpaceBusy = Error.Conflict(
        "Timetable.SpaceBusy",
        "El espacio ya está ocupado en esa franja");
}
