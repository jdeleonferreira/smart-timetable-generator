using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

public sealed class SubjectRepository : ISubjectRepository
{
    private readonly StgDbContext _db;
    public SubjectRepository(StgDbContext db) => _db = db;

    public Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Subjects.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Subject?> GetByNameAsync(string name, CancellationToken ct = default) =>
        _db.Subjects.FirstOrDefaultAsync(s => s.Name == name, ct);

    public async Task<IReadOnlyList<Subject>> ListAllAsync(CancellationToken ct = default) =>
        await _db.Subjects
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Subject>> SearchByPrefixAsync(string prefix, int max = 20, CancellationToken ct = default) =>
        await _db.Subjects
            .AsNoTracking()
            .Where(s => s.Name.StartsWith(prefix))
            .OrderBy(s => s.Name)
            .Take(max)
            .ToListAsync(ct);

    public Task AddAsync(Subject entity, CancellationToken ct = default)
    {
        _db.Subjects.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(Subject entity) => _db.Subjects.Update(entity);

    public void Remove(Subject entity) => _db.Subjects.Remove(entity);
}