// src/STG.Infrastructure/Persistence/Repositories/AssignmentRepository.cs
using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

internal sealed class AssignmentRepository : IAssignmentRepository
{
    private readonly StgDbContext _db;
    public AssignmentRepository(StgDbContext db) => _db = db;

    public async Task<Assignment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Assignment?> GetAsync(Guid groupId, Guid subjectId, Guid schoolYearId, CancellationToken ct = default)
        => await _db.Assignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.GroupId == groupId && a.SubjectId == subjectId && a.SchoolYearId == schoolYearId, ct);

    public async Task<List<Assignment>> ListByGroupAsync(Guid groupId, Guid schoolYearId, CancellationToken ct = default)
        => await _db.Assignments.AsNoTracking()
            .Where(a => a.GroupId == groupId && a.SchoolYearId == schoolYearId)
            .OrderBy(a => a.SubjectId)
            .ToListAsync(ct);

    public async Task<List<Assignment>> ListByTeacherAsync(Guid teacherId, Guid schoolYearId, CancellationToken ct = default)
        => await _db.Assignments.AsNoTracking()
            .Where(a => a.TeacherId == teacherId && a.SchoolYearId == schoolYearId)
            .OrderBy(a => a.GroupId).ThenBy(a => a.SubjectId)
            .ToListAsync(ct);

    public async Task<List<Assignment>> ListByYearAsync(Guid schoolYearId, CancellationToken ct = default)
        => await _db.Assignments.AsNoTracking()
            .Where(a => a.SchoolYearId == schoolYearId)
            .OrderBy(a => a.GroupId).ThenBy(a => a.SubjectId)
            .ToListAsync(ct);

    public async Task<Guid> AddAsync(Assignment entity, CancellationToken ct = default)
    {
        await _db.Assignments.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct); // auto-save
        return entity.Id;
    }

    public async Task UpsertAsync(Guid groupId, Guid subjectId, Guid schoolYearId, byte weeklyHours, Guid? teacherId, string? notes, CancellationToken ct = default)
    {
        var tracked = await _db.Assignments.FirstOrDefaultAsync(
            a => a.GroupId == groupId && a.SubjectId == subjectId && a.SchoolYearId == schoolYearId, ct);

        if (tracked is null)
        {
            var entity = new Assignment(Guid.NewGuid(), groupId, subjectId, schoolYearId, weeklyHours, teacherId, notes);
            await _db.Assignments.AddAsync(entity, ct);
        }
        else
        {
            tracked.SetWeeklyHours(weeklyHours);
            tracked.SetTeacher(teacherId);
            tracked.SetNotes(notes);
            _db.Assignments.Update(tracked);
        }
        await _db.SaveChangesAsync(ct); // auto-save
    }

    public async Task UpdateAsync(Assignment entity, CancellationToken ct = default)
    {
        _db.Assignments.Update(entity);
        await _db.SaveChangesAsync(ct); // auto-save
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tracked = await _db.Assignments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (tracked is null) return;
        _db.Assignments.Remove(tracked);
        await _db.SaveChangesAsync(ct); // auto-save
    }
}
