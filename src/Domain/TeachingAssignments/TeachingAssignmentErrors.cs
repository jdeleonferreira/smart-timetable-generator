namespace SmartTimetableGenerator.Domain.TeachingAssignments;

public static class TeachingAssignmentErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "TeachingAssignment.NotFound",
        "La asignación académica no existe");

    public static readonly Error Duplicate = Error.Conflict(
        "TeachingAssignment.Duplicate",
        "Ya existe una asignación para esa asignatura, curso y periodo");

    public static readonly Error ManualAssignmentLocked = Error.Conflict(
        "TeachingAssignment.ManualAssignmentLocked",
        "La asignación fue fijada a mano; libérela antes de proponer otro docente");

    public static readonly Error TeacherCannotTeachArea = Error.Validation(
        "TeachingAssignment.TeacherCannotTeachArea",
        "El docente no tiene asignada el área de la asignatura");

    public static readonly Error TeacherNotInCampus = Error.Validation(
        "TeachingAssignment.TeacherNotInCampus",
        "El docente no trabaja en la sede del curso");
}
