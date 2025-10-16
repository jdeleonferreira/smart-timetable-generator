using STG.Api.DTOs;
using STG.Domain.Entities;

namespace STG.Api.Mappings;

public static class SchedulingMappings
{
    public static AssignmentDto ToDto(this Assignment e) => new()
    {
        Id = e.Id,
        GroupId = e.GroupId,
        SubjectId = e.SubjectId,
        SchoolYearId = e.SchoolYearId,
        TeacherId = e.TeacherId,
        WeeklyHours = e.WeeklyHours,
        Notes = e.Notes
    };

    public static TimetableDto ToDto(this Timetable e) => new()
    {
        Id = e.Id,
        GroupId = e.GroupId,
        SchoolYearId = e.SchoolYearId,
        Name = e.Name,
        Notes = e.Notes,
        Entries = e.Entries.Select(x => x.ToDto()).ToList()
    };

    public static TimetableEntryDto ToDto(this TimetableEntry e) => new()
    {
        Id = e.Id,
        AssignmentId = e.AssignmentId,
        DayOfWeek = e.DayOfWeek,
        PeriodIndex = e.PeriodIndex,
        Span = e.Span,
        Room = e.Room,
        Notes = e.Notes
    };
}