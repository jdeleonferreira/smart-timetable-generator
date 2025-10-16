using System.ComponentModel.DataAnnotations;

namespace STG.Api.DTOs;

public sealed class TimetableDto
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public Guid SchoolYearId { get; init; }
    public string Name { get; init; } = null!;
    public string? Notes { get; init; }
    public List<TimetableEntryDto> Entries { get; init; } = new();
}

public sealed class TimetableEntryDto
{
    public Guid Id { get; init; }
    public Guid AssignmentId { get; init; }
    public byte DayOfWeek { get; init; }
    public byte PeriodIndex { get; init; }
    public byte Span { get; init; }
    public string? Room { get; init; }
    public string? Notes { get; init; }
}

public sealed class TimetableCreateRequest
{
    [Required] public Guid GroupId { get; set; }
    [Range(2000, 2100)] public int Year { get; set; }
    [MaxLength(80)] public string? Name { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}

public sealed class TimetableAddSlotRequest
{
    [Required] public Guid AssignmentId { get; set; }
    [Range(0, 6)] public byte DayOfWeek { get; set; }
    [Range(1, 24)] public byte PeriodIndex { get; set; }
    [Range(1, 8)] public byte Span { get; set; } = 1;
    [MaxLength(40)] public string? Room { get; set; }
    [MaxLength(250)] public string? Notes { get; set; }
}

public sealed class TimetableMoveSlotRequest
{
    [Range(0, 6)] public byte DayOfWeek { get; set; }
    [Range(1, 24)] public byte PeriodIndex { get; set; }
    [Range(1, 8)] public byte Span { get; set; } = 1;
}

public sealed class TimetableChangeAssignmentRequest { [Required] public Guid AssignmentId { get; set; } }
