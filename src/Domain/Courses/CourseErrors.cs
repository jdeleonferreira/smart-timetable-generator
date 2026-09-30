namespace SmartTimetableGenerator.Domain.Courses;

public static class CourseErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Course.NotFound",
        "El curso no existe");

    public static readonly Error DuplicateName = Error.Conflict(
        "Course.DuplicateName",
        "Ya existe un curso con ese nombre para el grado en la sede y año lectivo");

    public static readonly Error HomeRoomInOtherCampus = Error.Validation(
        "Course.HomeRoomInOtherCampus",
        "El salón pertenece a otra sede");

    public static readonly Error HomeRoomAlreadyAssigned = Error.Conflict(
        "Course.HomeRoomAlreadyAssigned",
        "El salón ya está asignado a otro curso de la misma jornada");
}
