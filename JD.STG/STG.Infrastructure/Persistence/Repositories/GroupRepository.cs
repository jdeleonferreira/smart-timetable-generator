using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

public sealed class GroupRepository : IGroupRepository
{
    private readonly StgDbContext _db;
    public GroupRepository(StgDbContext db) => _db = db;

    public Task<Group?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Groups.FirstOrDefaultAsync(g => g.Id == id, ct);

    public Task<Group?> GetByCodeAsync(Guid schoolYearId, string gradeName, string label, CancellationToken ct = default) =>
        _db.Groups.FirstOrDefaultAsync(g =>
            g.GradeName == gradeName && g.Label == label &&
            _db.Grades.Any(gr => gr.Id == g.GradeId && gr.SchoolYearId == schoolYearId), ct);

    public async Task<IReadOnlyList<Group>> ListByGradeAsync(Guid gradeId, CancellationToken ct = default) =>
        await _db.Groups.Where(g => g.GradeId == gradeId)
            .AsNoTracking()
            .OrderBy(g => g.Label)
            .ToListAsync(ct);

    public Task AddAsync(Group entity, CancellationToken ct = default)
    {
        _db.Groups.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(Group entity) => _db.Groups.Update(entity);
}