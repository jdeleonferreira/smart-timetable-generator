using STG.Api.DTOs;
using STG.Domain.Entities;

namespace STG.Api.Mappings;

public static class SchoolMappings
{
    public static GroupDto ToDto(this Group e) => new() { Id = e.Id, GradeId = e.GradeId, Name = e.Name };
}
