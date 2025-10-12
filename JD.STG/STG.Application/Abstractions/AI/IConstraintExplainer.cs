namespace STG.Application.Abstractions.AI;

/// <summary>
/// Explains scheduling constraints and decisions in human-readable text.
/// Infrastructure may implement it with LLMs or a no-op fallback.
/// </summary>
public interface IConstraintExplainer
{
    /// <summary>
    /// Builds an explanation string given hard/soft constraints and optional context.
    /// </summary>
    Task<string> ExplainAsync(
        IReadOnlyList<string> hardConstraints,
        IReadOnlyList<string> softConstraints,
        string? context = null,
        CancellationToken ct = default);
}
