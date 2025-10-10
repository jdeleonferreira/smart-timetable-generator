using STG.Domain.Entities;

namespace STG.Application.Abstractions.AI;

public interface IConstraintExplainer
{
    Task<string> ExplainAsync( Timetable partial, IEnumerable<string> violatedConstraints, CancellationToken ct = default);
}