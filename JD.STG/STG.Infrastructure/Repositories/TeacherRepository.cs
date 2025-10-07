using Microsoft.EntityFrameworkCore;
using STG.Application.Interfaces;
using STG.Domain.Entities;
using STG.Infrastructure.Persistence;

namespace STG.Infrastructure.Repositories;

public class TeacherRepository : ITeacherRepository
{
    private readonly StgDbContext _db;
    public TeacherRepository(StgDbContext db) => _db = db;

    public Task<Teacher?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Teachers.FindAsync(new object[] { id }, ct).AsTask();

    public async Task<IReadOnlyList<Teacher>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Teachers.AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Teacher>> GetQualifiedForAsync(string subject, CancellationToken ct)
        => await _db.Teachers.AsNoTracking()
            .Where(t => t.Subjects.Contains(subject))
            .ToListAsync(ct);

    public Task AddAsync(Teacher teacher, CancellationToken ct)
        => _db.Teachers.AddAsync(teacher, ct).AsTask();

    public Task AddRangeAsync(IEnumerable<Teacher> teachers, CancellationToken ct)
        => _db.Teachers.AddRangeAsync(teachers, ct);

    public void Remove(Teacher teacher) => _db.Teachers.Remove(teacher);
}
