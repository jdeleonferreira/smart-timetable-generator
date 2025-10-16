using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

internal sealed class GroupRepository : IGroupRepository
{
    private readonly StgDbContext _db;
    public GroupRepository(StgDbContext db) => _db = db;

    public async Task<Group?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<Group?> GetByGradeAndNameAsync(Guid gradeId, string name, CancellationToken ct = default)
        => await _db.Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.GradeId == gradeId && g.Name == name, ct);

    public async Task<List<Group>> ListByGradeAsync(Guid gradeId, CancellationToken ct = default)
        => await _db.Groups.AsNoTracking()
            .Where(g => g.GradeId == gradeId)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

    public async Task<List<Group>> ListAllAsync(CancellationToken ct = default)
        => await _db.Groups.AsNoTracking()
            .OrderBy(g => g.GradeId).ThenBy(g => g.Name)
            .ToListAsync(ct);

    public async Task<Guid> AddAsync(Group entity, CancellationToken ct = default)
    {
        await _db.Groups.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct); // auto-save
        return entity.Id;
    }

    public async Task UpdateAsync(Group entity, CancellationToken ct = default)
    {
        _db.Groups.Update(entity);
        await _db.SaveChangesAsync(ct); // auto-save
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tracked = await _db.Groups.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (tracked is null) return;
        _db.Groups.Remove(tracked);
        await _db.SaveChangesAsync(ct); // auto-save
    }

    public async Task AddRangeAsync(IEnumerable<Group> entities, CancellationToken ct = default)
    {
        await _db.Groups.AddRangeAsync(entities, ct);
        await _db.SaveChangesAsync(ct); // auto-save
    }
}
