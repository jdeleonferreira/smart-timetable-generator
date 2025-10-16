using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

internal sealed class TeacherRepository : ITeacherRepository
{
    private readonly StgDbContext _db;
    public TeacherRepository(StgDbContext db) => _db = db;

    public async Task<Teacher?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Teacher?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.Email == email, ct);

    public async Task<List<Teacher>> ListAllAsync(bool onlyActive, CancellationToken ct = default)
        => await _db.Teachers.AsNoTracking()
            .Where(t => !onlyActive || t.IsActive)
            .OrderBy(t => t.FullName)
            .ToListAsync(ct);

    public async Task<Guid> AddAsync(Teacher entity, CancellationToken ct = default)
    {
        await _db.Teachers.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(Teacher entity, CancellationToken ct = default)
    {
        _db.Teachers.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tracked = await _db.Teachers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (tracked is null) return;
        _db.Teachers.Remove(tracked);
        await _db.SaveChangesAsync(ct);
    }
}
