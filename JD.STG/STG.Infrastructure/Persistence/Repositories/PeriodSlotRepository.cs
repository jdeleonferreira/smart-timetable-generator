
using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

public sealed class PeriodSlotRepository : IPeriodSlotRepository
{
    private readonly StgDbContext _db;

    public PeriodSlotRepository(StgDbContext db) => _db = db;

    public async Task AddAsync(PeriodSlot slot, CancellationToken ct = default)
    {
        await _db.Set<PeriodSlot>().AddAsync(slot, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<PeriodSlot> slots, CancellationToken ct = default)
    {
        await _db.Set<PeriodSlot>().AddRangeAsync(slots, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(PeriodSlot slot, CancellationToken ct = default)
    {
        _db.Set<PeriodSlot>().Remove(slot);
        await _db.SaveChangesAsync(ct);
    }

    public Task<PeriodSlot?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Set<PeriodSlot>().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<IReadOnlyList<PeriodSlot>> ListBySchoolYearAsync(Guid schoolYearId, CancellationToken ct = default)
        => _db.Set<PeriodSlot>()
              .Where(x => x.SchoolYearId == schoolYearId)
              .OrderBy(x => x.DayOfWeek).ThenBy(x => x.PeriodNumber)
              .ToListAsync(ct).ContinueWith(t => (IReadOnlyList<PeriodSlot>)t.Result, ct);

    public Task<IReadOnlyList<PeriodSlot>> ListBySchoolYearAndDayAsync(Guid schoolYearId, int dayOfWeek, CancellationToken ct = default)
        => _db.Set<PeriodSlot>()
              .Where(x => x.SchoolYearId == schoolYearId && x.DayOfWeek == dayOfWeek)
              .OrderBy(x => x.PeriodNumber)
              .ToListAsync(ct).ContinueWith(t => (IReadOnlyList<PeriodSlot>)t.Result, ct);

    public Task<bool> ExistsAsync(Guid schoolYearId, int dayOfWeek, int periodNumber, CancellationToken ct = default)
        => _db.Set<PeriodSlot>()
              .AnyAsync(x => x.SchoolYearId == schoolYearId && x.DayOfWeek == dayOfWeek && x.PeriodNumber == periodNumber, ct);

    public async Task UpdateAsync(PeriodSlot slot, CancellationToken ct = default)
    {
        _db.Set<PeriodSlot>().Update(slot);
        await _db.SaveChangesAsync(ct);
    }
}