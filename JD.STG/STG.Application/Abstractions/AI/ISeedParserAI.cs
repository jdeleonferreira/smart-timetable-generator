namespace STG.Application.Abstractions.AI;

public interface ISeedParserAI
{
    Task<SeedResult> ParseAsync(SeedSource source, CancellationToken ct = default);
}
