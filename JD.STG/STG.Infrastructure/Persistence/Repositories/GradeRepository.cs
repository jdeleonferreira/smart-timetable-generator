using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

internal sealed class GradeRepository : IGradeRepository
{
    private readonly StgDbContext _db;
    public GradeRepository(StgDbContext db) => _db = db;

    public async Task<Grade?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Grades.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<Grade?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _db.Grades.AsNoTracking().FirstOrDefaultAsync(g => g.Name == name, ct);

    public async Task<Grade?> GetByOrderAsync(byte order, CancellationToken ct = default)
        => await _db.Grades.AsNoTracking().FirstOrDefaultAsync(g => g.Order == order, ct);

    public async Task<List<Grade>> ListAsync(CancellationToken ct = default)
        => await _db.Grades.AsNoTracking().OrderBy(g => g.Order).ToListAsync(ct);

    public async Task<Guid> AddAsync(Grade entity, CancellationToken ct = default)
    {
        await _db.Grades.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct); // auto-save
        return entity.Id;
    }

    public async Task UpdateAsync(Grade entity, CancellationToken ct = default)
    {
        _db.Grades.Update(entity);
        await _db.SaveChangesAsync(ct); // auto-save
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tracked = await _db.Grades.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (tracked is null) return;
        _db.Grades.Remove(tracked);
        await _db.SaveChangesAsync(ct); // auto-save
    }
}
