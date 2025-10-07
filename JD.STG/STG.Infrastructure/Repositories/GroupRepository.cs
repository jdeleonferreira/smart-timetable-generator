using Microsoft.EntityFrameworkCore;
using STG.Application.Interfaces;
using STG.Domain.Entities;
using STG.Infrastructure.Persistence;

namespace STG.Infrastructure.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly StgDbContext _db;
    public GroupRepository(StgDbContext db) => _db = db;

    public Task<Group?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Groups.FindAsync(new object[] { id }, ct).AsTask();

    public Task<Group?> GetByCodeAsync(string grade, string label, CancellationToken ct)
        => _db.Groups.FirstOrDefaultAsync(g => g.Grade == grade && g.Label == label, ct);

    public async Task<IReadOnlyList<Group>> GetAllAsync(CancellationToken ct)
        => await _db.Groups.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Group>> GetByGradeAsync(string grade, CancellationToken ct)
        => await _db.Groups.AsNoTracking().Where(g => g.Grade == grade).ToListAsync(ct);

    public Task AddAsync(Group group, CancellationToken ct)
        => _db.Groups.AddAsync(group, ct).AsTask();

    public Task AddRangeAsync(IEnumerable<Group> groups, CancellationToken ct)
        => _db.Groups.AddRangeAsync(groups, ct);

    public void Remove(Group group) => _db.Groups.Remove(group);
}
