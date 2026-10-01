namespace SmartTimetableGenerator.Domain.Teachers;

public static class TeacherErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Teacher.NotFound",
        "El docente no existe");

    public static readonly Error DuplicateEmail = Error.Conflict(
        "Teacher.DuplicateEmail",
        "Ya existe un docente con ese correo");

    public static readonly Error InvalidWorkload = Error.Validation(
        "Teacher.InvalidWorkload",
        "La carga no es válida: horas semanales entre 1 y 60, horas diarias entre 1 y la carga semanal, huecos no negativos");

    public static readonly Error AvailabilityOverlap = Error.Validation(
        "Teacher.AvailabilityOverlap",
        "Las reglas de disponibilidad se superponen en un mismo día");
}
