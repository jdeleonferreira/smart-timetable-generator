using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

internal sealed class StudyPlanEntryRepository : IStudyPlanEntryRepository
{
    private readonly StgDbContext _db;
    public StudyPlanEntryRepository(StgDbContext db) => _db = db;

    public Task<StudyPlanEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.StudyPlanEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<StudyPlanEntry?> FindAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, CancellationToken ct = default)
        => _db.StudyPlanEntries.AsNoTracking()
            .FirstOrDefaultAsync(e => e.StudyPlanId == studyPlanId && e.GradeId == gradeId && e.SubjectId == subjectId, ct);

    public Task<List<StudyPlanEntry>> ListByPlanAsync(Guid studyPlanId, CancellationToken ct = default)
        => _db.StudyPlanEntries.AsNoTracking()
            .Where(e => e.StudyPlanId == studyPlanId)
            .OrderBy(e => e.GradeId).ThenBy(e => e.SubjectId)
            .ToListAsync(ct);

    public Task<List<StudyPlanEntry>> ListByPlanAndGradeAsync(Guid studyPlanId, Guid gradeId, CancellationToken ct = default)
        => _db.StudyPlanEntries.AsNoTracking()
            .Where(e => e.StudyPlanId == studyPlanId && e.GradeId == gradeId)
            .OrderBy(e => e.SubjectId)
            .ToListAsync(ct);

    public async Task<Guid> AddAsync(StudyPlanEntry entity, CancellationToken ct = default)
    {
        await _db.StudyPlanEntries.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(StudyPlanEntry entity, CancellationToken ct = default)
    {
        _db.StudyPlanEntries.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tracked = await _db.StudyPlanEntries.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (tracked is null) return;
        _db.StudyPlanEntries.Remove(tracked);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteByKeyAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, CancellationToken ct = default)
    {
        var tracked = await _db.StudyPlanEntries
            .FirstOrDefaultAsync(x => x.StudyPlanId == studyPlanId && x.GradeId == gradeId && x.SubjectId == subjectId, ct);
        if (tracked is null) return;
        _db.StudyPlanEntries.Remove(tracked);
        await _db.SaveChangesAsync(ct);
    }
}
