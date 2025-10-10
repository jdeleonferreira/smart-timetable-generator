using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

public sealed class AssignmentRepository : IAssignmentRepository
{
    private readonly StgDbContext _db;

    public AssignmentRepository(StgDbContext db) => _db = db;

    public Task<Assignment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Assignments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> ExistsForGroupSubjectAsync(Guid schoolYearId, Guid groupId, Guid subjectId, CancellationToken ct = default) =>
        _db.Assignments.AnyAsync(a => a.SchoolYearId == schoolYearId && a.GroupId == groupId && a.SubjectId == subjectId, ct);

    public async Task<IReadOnlyList<Assignment>> ListByGroupAsync(Guid schoolYearId, Guid groupId, CancellationToken ct = default) =>
        await _db.Assignments
            .Where(a => a.SchoolYearId == schoolYearId && a.GroupId == groupId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Assignment>> ListByTeacherAsync(Guid schoolYearId, Guid teacherId, CancellationToken ct = default) =>
        await _db.Assignments
            .Where(a => a.SchoolYearId == schoolYearId && a.TeacherId == teacherId)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task AddAsync(Assignment entity, CancellationToken ct = default)
    {
        _db.Assignments.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(Assignment entity) => _db.Assignments.Update(entity);

    public void Remove(Assignment entity) => _db.Assignments.Remove(entity);
}
