using STG.Domain.Entities.Base;


namespace STG.Domain.Entities;

/// <summary>
/// Unidad atómica de horario: (día + número de periodo) con rango horario.
/// </summary>
public class PeriodSlot : Entity
{
    public Guid SchoolYearId { get; private set; }

    // 1 (Monday) .. 7 (Sunday) — mantenemos int para DB simple/portable
    public int DayOfWeek { get; private set; }

    // 1..N (típico 1..8)
    public int PeriodNumber { get; private set; }

    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }

    public bool IsBreak { get; private set; }
    public string? Label { get; private set; }

    // Concurrency
    public byte[]? RowVersion { get; private set; }

    private PeriodSlot() { } // EF

    private PeriodSlot(Guid schoolYearId, int dayOfWeek, int periodNumber, TimeOnly startTime, TimeOnly endTime, bool isBreak, string? label)
    {
        SetSchoolYear(schoolYearId);
        SetDayOfWeek(dayOfWeek);
        SetPeriodNumber(periodNumber);
        SetTimeRange(startTime, endTime);
        IsBreak = isBreak;
        SetLabel(label);
    }

    public static PeriodSlot Create(Guid schoolYearId, int dayOfWeek, int periodNumber, TimeOnly startTime, TimeOnly endTime, bool isBreak = false, string? label = null)
        => new(schoolYearId, dayOfWeek, periodNumber, startTime, endTime, isBreak, label);

    public void SetSchoolYear(Guid schoolYearId)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId requerido.");
        SchoolYearId = schoolYearId;
    }

    public void SetDayOfWeek(int dayOfWeek)
    {
        if (dayOfWeek < 1 || dayOfWeek > 7) throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "DayOfWeek debe estar en 1..7.");
        DayOfWeek = dayOfWeek;
    }

    public void SetPeriodNumber(int periodNumber)
    {
        if (periodNumber < 1 || periodNumber > 20) throw new ArgumentOutOfRangeException(nameof(periodNumber), "PeriodNumber fuera de rango razonable (1..20).");
        PeriodNumber = periodNumber;
    }

    public void SetTimeRange(TimeOnly start, TimeOnly end)
    {
        if (end <= start) throw new ArgumentException("EndTime debe ser mayor que StartTime.");
        var duration = end.ToTimeSpan() - start.ToTimeSpan();
        if (duration.TotalMinutes < 20 || duration.TotalMinutes > 120)
            throw new ArgumentOutOfRangeException(nameof(end), "Duración típica 20–120 min.");
        StartTime = start;
        EndTime = end;
    }

    public void MarkAsBreak(string? label = "Descanso")
    {
        IsBreak = true;
        SetLabel(label ?? "Descanso");
    }

    public void UnmarkBreak() => IsBreak = false;

    public void SetLabel(string? label)
    {
        if (!string.IsNullOrWhiteSpace(label) && label.Length > 64)
            throw new ArgumentException("Label máximo 64 caracteres.");
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
    }
}