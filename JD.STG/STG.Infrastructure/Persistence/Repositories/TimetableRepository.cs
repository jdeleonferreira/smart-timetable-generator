using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;
using STG.Domain.ValueObjects;

namespace STG.Infrastructure.Persistence.Repositories;

public sealed class TimetableRepository : ITimetableRepository
{
    private readonly StgDbContext _db;
    public TimetableRepository(StgDbContext db) => _db = db;

    public async Task<Timetable> GetOrCreateForYearAsync(Guid schoolYearId, CancellationToken ct = default)
    {
        var tt = await _db.Timetables.FirstOrDefaultAsync(t => t.SchoolYearId == schoolYearId, ct);
        if (tt is not null) return tt;

        tt = new Timetable(schoolYearId);
        _db.Timetables.Add(tt);
        await _db.SaveChangesAsync(ct);
        return tt;
    }

    public async Task<IReadOnlyList<TimetableEntry>> ListEntriesByGroupAsync(Guid schoolYearId, Guid groupId, CancellationToken ct = default) =>
        await _db.TimetableEntries
            .Where(e => e.SchoolYearId == schoolYearId && e.GroupId == groupId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TimetableEntry>> ListEntriesByTeacherAsync(Guid schoolYearId, Guid teacherId, CancellationToken ct = default) =>
        await _db.TimetableEntries
            .Where(e => e.SchoolYearId == schoolYearId && e.TeacherId == teacherId)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task AddEntryAsync(TimetableEntry entry, CancellationToken ct = default)
    {
        _db.TimetableEntries.Add(entry);
        return Task.CompletedTask;
    }

    public async Task RemoveEntryAsync(Guid entryId, CancellationToken ct = default)
    {
        var e = await _db.TimetableEntries.FirstOrDefaultAsync(x => x.Id == entryId, ct);
        if (e is not null) _db.TimetableEntries.Remove(e);
    }

    public Task<bool> IsGroupTakenAsync(Guid schoolYearId, Guid groupId, TimeSlot slot, CancellationToken ct = default) =>
        _db.TimetableEntries.AnyAsync(e =>
            e.SchoolYearId == schoolYearId &&
            e.GroupId == groupId &&
            e.Slot.Day == slot.Day &&
            e.Slot.Block == slot.Block, ct);

    public Task<bool> IsTeacherTakenAsync(Guid schoolYearId, Guid teacherId, TimeSlot slot, CancellationToken ct = default) =>
        _db.TimetableEntries.AnyAsync(e =>
            e.SchoolYearId == schoolYearId &&
            e.TeacherId == teacherId &&
            e.Slot.Day == slot.Day &&
            e.Slot.Block == slot.Block, ct);
}