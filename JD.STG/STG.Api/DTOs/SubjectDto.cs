using System.ComponentModel.DataAnnotations;

namespace STG.Contracts.Curriculum;

public sealed class SubjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Code { get; init; }
    public bool IsElective { get; init; }
    public Guid StudyAreaId { get; init; }
}

public sealed class SubjectCreateRequest
{
    [Required, MaxLength(120)] public string Name { get; set; } = null!;
    [Required] public Guid StudyAreaId { get; set; }
    [MaxLength(32)] public string? Code { get; set; }
    public bool IsElective { get; set; }
}

public sealed class SubjectRenameRequest
{
    [Required, MaxLength(120)] public string Name { get; set; } = null!;
}

public sealed class SubjectMoveRequest
{
    [Required] public Guid StudyAreaId { get; set; }
}
