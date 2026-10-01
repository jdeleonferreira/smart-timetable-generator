namespace SmartTimetableGenerator.Domain.Grades;

public static class GradeErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Grade.NotFound",
        "El grado no existe");

    public static readonly Error DuplicateName = Error.Conflict(
        "Grade.DuplicateName",
        "Ya existe un grado con ese nombre");
}
