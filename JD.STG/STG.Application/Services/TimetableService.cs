using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Scheduling;

/// <summary>
/// Application service to manage Timetables and enforce scheduling rules.
/// </summary>
public sealed class TimetableService
{
    private readonly ITimetableRepository _timetableRepository;
    private readonly ITimetableEntryRepository _timetableEntryRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ISchoolYearRepository _schoolYearRepository;

    public TimetableService(
        ITimetableRepository timetables,
        ITimetableEntryRepository entries,
        IAssignmentRepository assignments,
        ISchoolYearRepository years)
    {
        _timetableRepository = timetables;
        _timetableEntryRepository = entries;
        _assignmentRepository = assignments;
        _schoolYearRepository = years;
    }

    /// <summary>Creates a timetable for (Group, Year); idempotent on uniqueness.</summary>
    public async Task<Guid> CreateAsync(Guid groupId, int year, string? name = null, string? notes = null, CancellationToken ct = default)
    {
        var schoolYear = await _schoolYearRepository.GetByYearAsync(year, ct) ?? throw new KeyNotFoundException($"SchoolYear {year} not found.");

        var existing = await _timetableRepository.GetByGroupAndYearAsync(groupId, schoolYear.Id, ct);
        if (existing is not null) return existing.Id;

        var tt = new Timetable(Guid.NewGuid(), groupId, schoolYear.Id, name ?? $"Timetable {year}");
        tt.SetNotes(notes);
        return await _timetableRepository.AddAsync(tt, ct);
    }

    /// <summary>Adds a timeslot; validates overlap and assignment consistency.</summary>
    public async Task<Guid> AddSlotAsync(Guid timetableId, Guid assignmentId, byte dayOfWeek, byte periodIndex, byte span = 1, string? room = null, string? notes = null, CancellationToken ct = default)
    {
        var tt = await _timetableRepository.GetByIdAsync(timetableId, ct) ?? throw new KeyNotFoundException("Timetable not found.");
        var a = await _assignmentRepository.GetByIdAsync(assignmentId, ct) ?? throw new KeyNotFoundException("Assignment not found.");

        // Consistency: assignment must match timetable (Group & Year)
        if (a.GroupId != tt.GroupId || a.SchoolYearId != tt.SchoolYearId)
            throw new InvalidOperationException("Assignment does not belong to the timetable's group/year.");

        // Overlap: (Day, Period) must be free (for span, we check the first block + subsequent)
        await EnsureNoOverlapAsync(tt.Id, dayOfWeek, periodIndex, span, ct);

        // Weekly hours cap: count existing blocks for the assignment
        var currentBlocks = (await _timetableEntryRepository.ListByTimetableAsync(tt.Id, ct))
            .Count(e => e.AssignmentId == assignmentId);

        if (currentBlocks + span > a.WeeklyHours)
            throw new InvalidOperationException($"Adding {span} block(s) exceeds assignment weekly hours ({a.WeeklyHours}).");

        // Create entries for span (1 entry with span, o 1 por bloque; aquí guardamos span en la misma fila)
        var entry = new TimetableEntry(Guid.NewGuid(), tt.Id, assignmentId, dayOfWeek, periodIndex, span, room, notes);
        return await _timetableEntryRepository.AddAsync(entry, ct);
    }

    /// <summary>Moves a timeslot (keeps the same assignment).</summary>
    public async Task MoveSlotAsync(Guid entryId, byte newDayOfWeek, byte newPeriodIndex, byte newSpan, CancellationToken ct = default)
    {
        var entry = await _timetableEntryRepository.GetByIdAsync(entryId, ct) ?? throw new KeyNotFoundException("TimetableEntry not found.");
        // Conflict check at destination
        await EnsureNoOverlapAsync(entry.TimetableId, newDayOfWeek, newPeriodIndex, newSpan, ct, ignoreEntryId: entry.Id);

        entry.SetDayAndPeriod(newDayOfWeek, newPeriodIndex, newSpan);
        await _timetableEntryRepository.UpdateAsync(entry, ct);
    }

    /// <summary>Changes the assignment for a given slot; validates group/year + hours cap.</summary>
    public async Task ChangeAssignmentAsync(Guid entryId, Guid newAssignmentId, CancellationToken ct = default)
    {
        var entry = await _timetableEntryRepository.GetByIdAsync(entryId, ct) ?? throw new KeyNotFoundException("TimetableEntry not found.");
        var tt = await _timetableRepository.GetByIdAsync(entry.TimetableId, ct) ?? throw new KeyNotFoundException("Timetable not found.");
        var a = await _assignmentRepository.GetByIdAsync(newAssignmentId, ct) ?? throw new KeyNotFoundException("Assignment not found.");

        if (a.GroupId != tt.GroupId || a.SchoolYearId != tt.SchoolYearId)
            throw new InvalidOperationException("Assignment does not belong to the timetable's group/year.");

        // Weekly hours cap for new assignment
        var currentBlocks = (await _timetableEntryRepository.ListByTimetableAsync(tt.Id, ct))
            .Where(e => e.Id != entry.Id && e.AssignmentId == newAssignmentId)
            .Sum(e => (int)e.Span);

        if (currentBlocks + entry.Span > a.WeeklyHours)
            throw new InvalidOperationException($"Changing would exceed weekly hours ({a.WeeklyHours}).");

        // Recreate entry to change FK safely (or add a setter if prefieres)
        var updated = new TimetableEntry(entry.Id, entry.TimetableId, newAssignmentId, entry.DayOfWeek, entry.PeriodIndex, entry.Span, entry.Room, entry.Notes);
        await _timetableEntryRepository.UpdateAsync(updated, ct);
    }

    /// <summary>Removes a timeslot.</summary>
    public async Task RemoveSlotAsync(Guid entryId, CancellationToken ct = default)
        => await _timetableEntryRepository.DeleteAsync(entryId, ct);

    private async Task EnsureNoOverlapAsync(Guid timetableId, byte dayOfWeek, byte periodIndex, byte span, CancellationToken ct, Guid? ignoreEntryId = null)
    {
        var list = await _timetableEntryRepository.ListByTimetableAsync(timetableId, ct);
        // For simplicity, consider overlap if any entry shares the same (day, period) regardless of span
        // (si quieres granularidad por span, extiende a rango [period, period+span-1]).
        for (int i = 0; i < span; i++)
        {
            var p = (byte)(periodIndex + i);
            var overlap = list.Any(e =>
                e.DayOfWeek == dayOfWeek &&
                e.PeriodIndex == p &&
                (!ignoreEntryId.HasValue || e.Id != ignoreEntryId.Value));

            if (overlap)
                throw new InvalidOperationException($"Timeslot overlap at day {dayOfWeek}, period {p}.");
        }
    }


    public async Task<Timetable?> GetByIdAsync(Guid timetableId, CancellationToken ct = default)
    => await _timetableRepository.GetByIdAsync(timetableId, ct);

    public async Task<Timetable?> GetByGroupAndYearAsync(Guid groupId, int year, CancellationToken ct = default)
    {
        var sy = await _schoolYearRepository.GetByYearAsync(year, ct);
        if (sy is null) return null;
        return await _timetableRepository.GetByGroupAndYearAsync(groupId, sy.Id, ct);
    }
}
