namespace STG.Application.Abstractions.Seeding;

/// <summary>Simple outcome summary for data seeding.</summary>
public sealed class SeedResult
{
    public SeedSource Source { get; }
    public int Created { get; }
    public int Updated { get; }
    public int Skipped { get; }
    public string? Notes { get; }

    public SeedResult(SeedSource source, int created, int updated, int skipped, string? notes = null)
    {
        Source = source;
        Created = created;
        Updated = updated;
        Skipped = skipped;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public static SeedResult Empty(SeedSource source, string? notes = null)
        => new(source, 0, 0, 0, notes);
}
