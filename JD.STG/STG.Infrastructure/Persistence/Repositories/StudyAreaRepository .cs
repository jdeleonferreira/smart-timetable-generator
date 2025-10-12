using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IStudyAreaRepository"/>.</summary>
internal sealed class StudyAreaRepository : IStudyAreaRepository
{
    private readonly StgDbContext _db;
    public StudyAreaRepository(StgDbContext db) => _db = db;

    public async Task<StudyArea?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.StudyAreas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<StudyArea?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _db.StudyAreas.AsNoTracking().FirstOrDefaultAsync(a => a.Name == name, ct);

    public async Task<List<StudyArea>> ListAsync(CancellationToken ct = default)
        => await _db.StudyAreas.AsNoTracking()
            .OrderBy(a => a.OrderNo).ThenBy(a => a.Name)
            .ToListAsync(ct);

    public async Task<Guid> AddAsync(StudyArea entity, CancellationToken ct = default)
    {
        await _db.StudyAreas.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(StudyArea entity, CancellationToken ct = default)
    {
        _db.StudyAreas.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tracked = await _db.StudyAreas.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (tracked is null) return;
        _db.StudyAreas.Remove(tracked);
        await _db.SaveChangesAsync(ct);
    }
}
