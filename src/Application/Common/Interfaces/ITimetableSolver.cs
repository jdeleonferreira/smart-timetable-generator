using SmartTimetableGenerator.Application.Common.Scheduling;

namespace SmartTimetableGenerator.Application.Common.Interfaces;

/// <summary>
/// Motor de programación de horarios (implementado con Google OR-Tools CP-SAT en Infrastructure).
/// </summary>
public interface ITimetableSolver
{
    Task<SchedulingSolution> SolveAsync(SchedulingProblem problem, CancellationToken cancellationToken);
}
