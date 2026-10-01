namespace SmartTimetableGenerator.Domain.Institutions;

public static class InstitutionErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Institution.NotFound",
        "La institución no existe");

    public static readonly Error AlreadyExists = Error.Conflict(
        "Institution.AlreadyExists",
        "Ya existe una institución registrada");
}
