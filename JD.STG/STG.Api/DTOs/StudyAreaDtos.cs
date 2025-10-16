using System.ComponentModel.DataAnnotations;

namespace STG.Api.DTOs;

public sealed class StudyAreaDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Code { get; init; }
    public byte? OrderNo { get; init; }
    public bool IsActive { get; init; }
}

public sealed class StudyAreaCreateRequest
{
    [Required, MaxLength(100)] public string Name { get; set; } = null!;
    [MaxLength(32)] public string? Code { get; set; }
    [Range(0, 255)] public byte OrderNo { get; set; }
}

public sealed class StudyAreaUpdateRequest
{
    [Required, MaxLength(100)] public string Name { get; set; } = null!;
    [MaxLength(32)] public string? Code { get; set; }
    [Range(0, 255)] public byte OrderNo { get; set; }
    public bool IsActive { get; set; }
}
