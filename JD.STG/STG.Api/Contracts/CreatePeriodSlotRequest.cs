namespace STG.Api.Contracts;

public sealed class CreatePeriodSlotRequest
{
    public Guid SchoolYearId { get; init; }
    public int DayOfWeek { get; init; }
    public int PeriodNumber { get; init; }
    public string StartTime { get; init; } = default!; // "07:00"
    public string EndTime { get; init; } = default!;
    public bool IsBreak { get; init; }
    public string? Label { get; init; }
}