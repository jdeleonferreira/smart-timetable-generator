namespace STG.Infrastructure.AI.OpenAI;

/// <summary>Options for the OpenAI-based constraint explainer.</summary>
public sealed class OpenAIOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
    public int? MaxTokens { get; set; } = 512;
}