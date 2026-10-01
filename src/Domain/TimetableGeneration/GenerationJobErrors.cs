namespace SmartTimetableGenerator.Domain.TimetableGeneration;

public static class GenerationJobErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "GenerationJob.NotFound",
        "La solicitud de generación no existe");

    public static readonly Error AlreadyRunning = Error.Conflict(
        "GenerationJob.AlreadyRunning",
        "Ya hay una generación en curso para este horario");

    public static readonly Error InvalidTransition = Error.Conflict(
        "GenerationJob.InvalidTransition",
        "La solicitud de generación no está en un estado que permita esta acción");
}
