namespace SmartTimetableGenerator.Domain.TrainingProjects;

public static class TrainingProjectErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "TrainingProject.NotFound",
        "El proyecto de formación no existe");

    public static readonly Error ActivityNotFound = Error.NotFound(
        "TrainingProject.ActivityNotFound",
        "La actividad no existe en el proyecto");

    public static readonly Error InvalidActivityTime = Error.Validation(
        "TrainingProject.InvalidActivityTime",
        "La hora final de la actividad debe ser posterior a la inicial");

    public static readonly Error ActivityOutsideYear = Error.Validation(
        "TrainingProject.ActivityOutsideYear",
        "La fecha de la actividad está fuera del año lectivo");
}
