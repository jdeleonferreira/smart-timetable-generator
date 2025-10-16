using STG.Api.Contracts;
using STG.Api.DTOs;
using STG.Domain.Entities;

namespace STG.Api.Mappings;

public static class PeriodSlotMappings
{

    public static PeriodSlotDto ToDto(this PeriodSlot x) => new()
    {
        Id = x.Id,
        SchoolYearId = x.SchoolYearId,
        DayOfWeek = x.DayOfWeek,
        PeriodNumber = x.PeriodNumber,
        StartTime = x.StartTime.ToString("HH:mm"),
        EndTime = x.EndTime.ToString("HH:mm"),
        IsBreak = x.IsBreak,
        Label = x.Label
    };

    public static IEnumerable<PeriodSlotDto> ToDto(this IEnumerable<PeriodSlot> items)
        => items.Select(ToDto);

    // DTOs → parámetros de servicio (helpers de parsing)
    public static (Guid SchoolYearId, int DayOfWeek, int PeriodNumber, TimeOnly Start, TimeOnly End, bool IsBreak, string? Label)
        ToCreateParams(this CreatePeriodSlotRequest r)
        => (r.SchoolYearId, r.DayOfWeek, r.PeriodNumber, TimeOnly.Parse(r.StartTime), TimeOnly.Parse(r.EndTime), r.IsBreak, r.Label);

    public static (TimeOnly Start, TimeOnly End, bool IsBreak, string? Label)
        ToUpdateParams(this UpdatePeriodSlotRequest r)
        => (TimeOnly.Parse(r.StartTime), TimeOnly.Parse(r.EndTime), r.IsBreak, r.Label);
}
