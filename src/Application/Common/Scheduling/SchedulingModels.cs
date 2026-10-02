namespace SmartTimetableGenerator.Application.Common.Scheduling;

/// <summary>
/// Problema de programación independiente del motor: ubicar horas de clase en una cuadrícula días × franjas.
/// Los recursos (curso, docente, espacio) se identifican con claves de texto; un recurso solo puede
/// tener una clase por franja.
/// </summary>
public sealed record SchedulingProblem(
    int Days,
    int PeriodsPerDay,
    IReadOnlyList<SchedulingItem> Items,
    IReadOnlyList<BlockedSlot> BlockedSlots,
    IReadOnlyList<DailyLoadRule> DailyLoads,
    int TimeLimitSeconds,
    IReadOnlyList<BlockedSlot>? AvoidedSlots = null);

/// <summary>
/// Una asignatura en un curso con sus horas semanales y los recursos que ocupa.
/// </summary>
public sealed record SchedulingItem(
    int Index,
    string CourseKey,
    IReadOnlyList<string> ResourceKeys,
    int Hours,
    int MaxPerDay,
    int MaxConsecutive,
    bool RequireContiguousPair,
    IReadOnlyList<SlotRef> FixedSlots);

public readonly record struct SlotRef(int Day, int Period);

/// <summary>
/// Franja en la que un recurso no puede tener clase (disponibilidad del docente, clases en otra sede…).
/// También describe las franjas que un recurso prefiere evitar (<see cref="SchedulingProblem.AvoidedSlots"/>): el motor
/// las usa solo si no hay otra opción.
/// </summary>
public sealed record BlockedSlot(string ResourceKey, int Day, int Period);

/// <summary>
/// Límites de horas por día de un recurso (carga pareja del curso, máximo diario del docente).
/// El máximo es estricto; el mínimo es una preferencia. Si <paramref name="Day"/> tiene valor, aplica solo a ese día.
/// </summary>
public sealed record DailyLoadRule(string ResourceKey, int MinPerDay, int MaxPerDay, int? Day = null);

public enum SchedulingStatus
{
    Optimal,
    Feasible,
    Infeasible,
    Unknown
}

public sealed record SchedulingSolution(
    SchedulingStatus Status,
    IReadOnlyList<PlacedHour> Placed,
    IReadOnlyDictionary<int, int> UnplacedHoursByItem,
    double SolveSeconds);

public readonly record struct PlacedHour(int ItemIndex, int Day, int Period);
