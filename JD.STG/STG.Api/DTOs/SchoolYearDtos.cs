using System.ComponentModel.DataAnnotations;

namespace STG.Api.DTOs;

public sealed class SchoolYearDto { public Guid Id { get; init; } public int Year { get; init; } }
public sealed class SchoolYearCreateRequest { [Range(2000, 2100)] public int Year { get; set; } }
