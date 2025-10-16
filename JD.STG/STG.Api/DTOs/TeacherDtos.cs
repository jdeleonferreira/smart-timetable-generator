using System.ComponentModel.DataAnnotations;

namespace STG.Api.DTOs;

public sealed class TeacherDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public string? Email { get; init; }
    public byte? MaxWeeklyLoad { get; init; }
    public bool IsActive { get; init; }
}

public sealed class TeacherCreateRequest
{
    [Required, MaxLength(120)] public string FullName { get; set; } = null!;
    [EmailAddress, MaxLength(120)] public string? Email { get; set; }
    [Range(1, 40)] public byte? MaxWeeklyLoad { get; set; }
}

public sealed class TeacherRenameRequest { [Required, MaxLength(120)] public string FullName { get; set; } = null!; }
public sealed class TeacherEmailRequest { [EmailAddress, MaxLength(120)] public string? Email { get; set; } }
public sealed class TeacherMaxLoadRequest { [Range(1, 40)] public byte? MaxWeeklyLoad { get; set; } }
