using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

internal sealed class TimetableEntryRepository : ITimetableEntryRepository
{
    private readonly StgDbContext _db;
    public TimetableEntryRepository(StgDbContext db) => _db = db;

    public async Task<TimetableEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.TimetableEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<TimetableEntry?> FindAsync(Guid timetableId, byte dayOfWeek, byte periodIndex, CancellationToken ct = default)
        => await _db.TimetableEntries.AsNoTracking()
            .FirstOrDefaultAsync(e => e.TimetableId == timetableId && e.DayOfWeek == dayOfWeek && e.PeriodIndex == periodIndex, ct);

    public async Task<List<TimetableEntry>> ListByTimetableAsync(Guid timetableId, CancellationToken ct = default)
        => await _db.TimetableEntries.AsNoTracking()
            .Where(e => e.TimetableId == timetableId)
            .OrderBy(e => e.DayOfWeek).ThenBy(e => e.PeriodIndex)
            .ToListAsync(ct);

    public async Task<Guid> AddAsync(TimetableEntry entity, CancellationToken ct = default)
    {
        await _db.TimetableEntries.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(TimetableEntry entity, CancellationToken ct = default)
    {
        _db.TimetableEntries.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tracked = await _db.TimetableEntries.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (tracked is null) return;
        _db.TimetableEntries.Remove(tracked);
        await _db.SaveChangesAsync(ct);
    }
}