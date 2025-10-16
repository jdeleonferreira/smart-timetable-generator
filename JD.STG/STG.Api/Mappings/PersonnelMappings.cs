using STG.Api.DTOs;
using STG.Domain.Entities;

namespace STG.Api.Mappings;

public static class PersonnelMappings
{
    public static TeacherDto ToDto(this Teacher e) => new()
    {
        Id = e.Id,
        FullName = e.FullName,
        Email = e.Email,
        MaxWeeklyLoad = e.MaxWeeklyLoad,
        IsActive = e.IsActive
    };
}