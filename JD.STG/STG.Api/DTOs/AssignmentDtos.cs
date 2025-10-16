using System.ComponentModel.DataAnnotations;

namespace STG.Api.DTOs;

public sealed class AssignmentDto
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public Guid SubjectId { get; init; }
    public Guid SchoolYearId { get; init; }
    public Guid? TeacherId { get; init; }
    public byte WeeklyHours { get; init; }
    public string? Notes { get; init; }
}

public sealed class AssignmentUpsertRequest
{
    [Required] public Guid GroupId { get; set; }
    [Required] public Guid SubjectId { get; set; }
    [Range(2000, 2100)] public int Year { get; set; }
    [Range(0, 40)] public byte WeeklyHours { get; set; }
    public Guid? TeacherId { get; set; }
    [MaxLength(300)] public string? Notes { get; set; }
}

public sealed class AssignmentSetTeacherRequest { public Guid? TeacherId { get; set; } }
public sealed class AssignmentSetHoursRequest { [Range(0, 40)] public byte WeeklyHours { get; set; } }
