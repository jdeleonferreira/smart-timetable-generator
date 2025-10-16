namespace STG.Api.Contracts;

public sealed class UpdatePeriodSlotRequest
{
    public string StartTime { get; init; } = default!;
    public string EndTime { get; init; } = default!;
    public bool IsBreak { get; init; }
    public string? Label { get; init; }
}