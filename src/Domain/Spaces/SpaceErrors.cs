namespace SmartTimetableGenerator.Domain.Spaces;

public static class SpaceErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Space.NotFound",
        "El espacio no existe");

    public static readonly Error DuplicateName = Error.Conflict(
        "Space.DuplicateName",
        "Ya existe un espacio con ese nombre en la sede");
}
