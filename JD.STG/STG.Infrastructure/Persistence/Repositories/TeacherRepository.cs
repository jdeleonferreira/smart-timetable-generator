using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

public sealed class TeacherRepository : ITeacherRepository
{
    private readonly StgDbContext _db;
    public TeacherRepository(StgDbContext db) => _db = db;

    public Task<Teacher?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Teachers.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Teacher?> GetByNameAsync(string name, CancellationToken ct = default) =>
        _db.Teachers.FirstOrDefaultAsync(t => t.Name == name, ct);

    public async Task<IReadOnlyList<Teacher>> ListAllAsync(CancellationToken ct = default) =>
        await _db.Teachers
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    /// <summary>
    /// Returns teachers qualified for the given subject name.
    /// Note: Subjects are stored as JSON (backing field) in MVP; this filters in-memory.
    /// For large datasets, normalize to a join table or map by SubjectId.
    /// </summary>
    public async Task<IReadOnlyList<Teacher>> ListQualifiedForAsync(string subjectName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(subjectName)) return Array.Empty<Teacher>();

        // Due to JSON storage of _subjects, filter client-side (MVP trade-off).
        var all = await _db.Teachers.AsNoTracking().ToListAsync(ct);
        return all.Where(t => t.Subjects.Any(s => string.Equals(s, subjectName, StringComparison.OrdinalIgnoreCase)))
                  .OrderBy(t => t.Name)
                  .ToList();
    }

    public Task AddAsync(Teacher entity, CancellationToken ct = default)
    {
        _db.Teachers.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(Teacher entity) => _db.Teachers.Update(entity);

    public void Remove(Teacher entity) => _db.Teachers.Remove(entity);
}