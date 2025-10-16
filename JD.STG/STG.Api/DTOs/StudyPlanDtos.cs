using System.ComponentModel.DataAnnotations;

namespace STG.Api.DTOs;

public sealed class StudyPlanDto
{
    public Guid Id { get; init; }
    public Guid SchoolYearId { get; init; }
    public string Name { get; init; } = null!;
    public string? Notes { get; init; }
    public List<StudyPlanEntryDto> Entries { get; init; } = new();
}
public sealed class StudyPlanEntryDto
{
    public Guid Id { get; init; }
    public Guid GradeId { get; init; }
    public Guid SubjectId { get; init; }
    public byte WeeklyHours { get; init; }
    public string? Notes { get; init; }
}

public sealed class StudyPlanCreateForYearRequest
{
    [Range(2000, 2100)] public int Year { get; set; }
    [MaxLength(80)] public string? Name { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}

public sealed class StudyPlanUpsertEntryRequest
{
    [Required] public Guid GradeId { get; set; }
    [Required] public Guid SubjectId { get; set; }
    [Range(0, 40)] public byte WeeklyHours { get; set; }
    [MaxLength(250)] public string? Notes { get; set; }
}

public sealed class StudyPlanRemoveEntryRequest
{
    [Required] public Guid GradeId { get; set; }
    [Required] public Guid SubjectId { get; set; }
}