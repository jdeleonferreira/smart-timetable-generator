using Microsoft.Extensions.Options;
using STG.Application.Abstractions.AI;

namespace STG.Infrastructure.AI.OpenAI;

/// <summary>
/// OpenAI-backed explainer. This stub compiles without calling external APIs.
/// Later you can inject an SDK client and implement the real call.
/// </summary>
public sealed class OpenAIConstraintExplainer : IConstraintExplainer
{
    private readonly OpenAIOptions _options;

    public OpenAIConstraintExplainer(IOptions<OpenAIOptions> options)
        => _options = options.Value;

    public Task<string> ExplainAsync(
        IReadOnlyList<string> hardConstraints,
        IReadOnlyList<string> softConstraints,
        string? context = null,
        CancellationToken ct = default)
    {
        // Minimal, deterministic text to keep builds green.
        var hard = hardConstraints?.Count > 0 ? string.Join("; ", hardConstraints) : "none";
        var soft = softConstraints?.Count > 0 ? string.Join("; ", softConstraints) : "none";
        var header = $"[Model={_options.Model ?? "N/A"} MaxTokens={_options.MaxTokens?.ToString() ?? "N/A"}]";
        var ctx = string.IsNullOrWhiteSpace(context) ? "" : $"\nContext: {context}";
        var msg = $"{header}\nHard: {hard}\nSoft: {soft}{ctx}";
        return Task.FromResult(msg);
    }
}
