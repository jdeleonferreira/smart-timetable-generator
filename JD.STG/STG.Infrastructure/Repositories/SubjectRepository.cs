using Microsoft.EntityFrameworkCore;
using STG.Application.Interfaces;
using STG.Domain.Entities;
using STG.Infrastructure.Persistence;

namespace STG.Infrastructure.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly StgDbContext _db;
    public SubjectRepository(StgDbContext db) => _db = db;

    public Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Subjects.FindAsync(new object[] { id }, ct).AsTask();

    public Task<Subject?> GetByNameAsync(string name, CancellationToken ct)
        => _db.Subjects.FirstOrDefaultAsync(s => s.Name == name, ct);

    public async Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken ct)
        => await _db.Subjects.AsNoTracking().ToListAsync(ct);

    public Task AddAsync(Subject subject, CancellationToken ct)
        => _db.Subjects.AddAsync(subject, ct).AsTask();

    public Task AddRangeAsync(IEnumerable<Subject> subjects, CancellationToken ct)
        => _db.Subjects.AddRangeAsync(subjects, ct);

    public void Remove(Subject subject) => _db.Subjects.Remove(subject);
}
