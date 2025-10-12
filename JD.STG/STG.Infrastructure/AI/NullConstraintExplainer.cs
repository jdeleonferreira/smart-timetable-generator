using STG.Application.Abstractions.AI;

namespace STG.Infrastructure.AI;

/// <summary>No-op explainer that returns a terse placeholder text.</summary>
public sealed class NullConstraintExplainer : IConstraintExplainer
{
    public Task<string> ExplainAsync(
        IReadOnlyList<string> hardConstraints,
        IReadOnlyList<string> softConstraints,
        string? context = null,
        CancellationToken ct = default)
        => Task.FromResult("Constraint explainer is disabled.");
}
