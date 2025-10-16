using System.ComponentModel.DataAnnotations;

namespace STG.Api.DTOs;

public sealed class GradeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public byte Order { get; init; }
}

public sealed class GradeCreateRequest
{
    [Required, MaxLength(32)] public string Name { get; set; } = null!;
    [Range(0, 255)] public byte Order { get; set; }
}

public sealed class GradeRenameRequest { [Required, MaxLength(32)] public string Name { get; set; } = null!; }
public sealed class GradeReorderRequest { [Range(0, 255)] public byte Order { get; set; } }