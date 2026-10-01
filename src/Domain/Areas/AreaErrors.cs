namespace SmartTimetableGenerator.Domain.Areas;

public static class AreaErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Area.NotFound",
        "El área no existe");

    public static readonly Error DuplicateName = Error.Conflict(
        "Area.DuplicateName",
        "Ya existe un área con ese nombre");

    public static readonly Error SubjectNotFound = Error.NotFound(
        "Area.SubjectNotFound",
        "La asignatura no existe en esta área");

    public static readonly Error DuplicateSubjectName = Error.Conflict(
        "Area.DuplicateSubjectName",
        "Ya existe una asignatura con ese nombre en el área");
}
