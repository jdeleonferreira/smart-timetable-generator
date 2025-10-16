using STG.Api.DTOs;
using STG.Contracts.Curriculum;
using STG.Domain.Entities;

namespace STG.Api.Mappings;
public static class CurriculumMappings
{
    public static StudyAreaDto ToDto(this StudyArea e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Code = e.Code,
        OrderNo = e.OrderNo,
        IsActive = e.IsActive
    };

    public static SubjectDto ToDto(this Subject e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Code = e.Code,
        IsElective = e.IsElective,
        StudyAreaId = e.StudyAreaId
    };

    public static GradeDto ToDto(this Grade e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Order = e.Order
    };

    public static SchoolYearDto ToDto(this SchoolYear e) => new() { Id = e.Id, Year = e.Year };

    public static StudyPlanDto ToDto(this StudyPlan e) => new()
    {
        Id = e.Id,
        SchoolYearId = e.SchoolYearId,
        Name = e.Name,
        Notes = e.Notes,
        Entries = e.Entries.Select(x => x.ToDto()).ToList()
    };

    public static StudyPlanEntryDto ToDto(this StudyPlanEntry e) => new()
    {
        Id = e.Id,
        GradeId = e.GradeId,
        SubjectId = e.SubjectId,
        WeeklyHours = e.WeeklyHours,
        Notes = e.Notes
    };
}