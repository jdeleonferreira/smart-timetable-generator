using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

internal sealed class TimetableRepository : ITimetableRepository
{
    private readonly StgDbContext _db;
    public TimetableRepository(StgDbContext db) => _db = db;

    public async Task<Timetable?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Timetables.Include(t => t.Entries).AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Timetable?> GetByGroupAndYearAsync(Guid groupId, Guid schoolYearId, CancellationToken ct = default)
        => await _db.Timetables.Include(t => t.Entries).AsNoTracking()
            .FirstOrDefaultAsync(t => t.GroupId == groupId && t.SchoolYearId == schoolYearId, ct);

    public async Task<List<Timetable>> ListByYearAsync(Guid schoolYearId, CancellationToken ct = default)
        => await _db.Timetables.AsNoTracking()
            .Where(t => t.SchoolYearId == schoolYearId)
            .OrderBy(t => t.GroupId)
            .ToListAsync(ct);

    public async Task<Guid> AddAsync(Timetable entity, CancellationToken ct = default)
    {
        await _db.Timetables.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(Timetable entity, CancellationToken ct = default)
    {
        _db.Timetables.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tracked = await _db.Timetables.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (tracked is null) return;
        _db.Timetables.Remove(tracked);
        await _db.SaveChangesAsync(ct);
    }
}
