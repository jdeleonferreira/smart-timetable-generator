using System.ComponentModel.DataAnnotations;

namespace STG.Api.DTOs;

public sealed class GroupDto
{
    public Guid Id { get; init; }
    public Guid GradeId { get; init; }
    public string Name { get; init; } = null!;
}

public sealed class GroupCreateRequest
{
    [Required] public Guid GradeId { get; set; }
    [Required, MaxLength(16)] public string Name { get; set; } = null!;
}
public sealed class GroupBulkCreateRequest
{
    [Required] public Guid GradeId { get; set; }
    [Required] public List<string> Names { get; set; } = new();
}
public sealed class GroupRenameRequest { [Required, MaxLength(16)] public string Name { get; set; } = null!; }