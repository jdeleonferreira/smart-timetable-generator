namespace SmartTimetableGenerator.Domain.StudyPlans;

public static class StudyPlanErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "StudyPlan.NotFound",
        "El plan de estudios no existe");

    public static readonly Error AlreadyExists = Error.Conflict(
        "StudyPlan.AlreadyExists",
        "Ya existe un plan de estudios para esa sede y año lectivo");

    public static readonly Error NotEditable = Error.Conflict(
        "StudyPlan.NotEditable",
        "El plan está aprobado; reábralo para modificarlo");

    public static readonly Error AlreadyApproved = Error.Conflict(
        "StudyPlan.AlreadyApproved",
        "El plan ya está aprobado");

    public static readonly Error Empty = Error.Validation(
        "StudyPlan.Empty",
        "El plan no tiene asignaturas");

    public static readonly Error ItemNotFound = Error.NotFound(
        "StudyPlan.ItemNotFound",
        "La asignatura no está en el plan");

    public static readonly Error DuplicateItem = Error.Conflict(
        "StudyPlan.DuplicateItem",
        "La asignatura ya está en el plan para ese grado");

    public static readonly Error InvalidWeeklyHours = Error.Validation(
        "StudyPlan.InvalidWeeklyHours",
        "La intensidad horaria debe estar entre 0 y 40 horas semanales");

    public static readonly Error CounterShiftRequiresShift = Error.Validation(
        "StudyPlan.CounterShiftRequiresShift",
        "Una asignatura en contrajornada debe indicar la jornada donde se dicta");

    public static readonly Error IntegratedIntoItself = Error.Validation(
        "StudyPlan.IntegratedIntoItself",
        "Una asignatura transversal no puede integrarse en sí misma");

    public static readonly Error TransversalHasNoHours = Error.Validation(
        "StudyPlan.TransversalHasNoHours",
        "Una asignatura transversal no tiene intensidad horaria");

    public static readonly Error InvalidDistribution = Error.Validation(
        "StudyPlan.InvalidDistribution",
        "La distribución no es válida: los máximos deben ser positivos y las horas seguidas no pueden superar las horas por día");
}
