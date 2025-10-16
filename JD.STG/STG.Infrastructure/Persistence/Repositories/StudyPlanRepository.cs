using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

internal sealed class StudyPlanRepository : IStudyPlanRepository
{
    private readonly StgDbContext _db;
    public StudyPlanRepository(StgDbContext db) => _db = db;

    public Task<StudyPlan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.StudyPlans
              .Include(p => p.Entries)
              .AsNoTracking()
              .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<StudyPlan?> GetByYearAsync(int year, CancellationToken ct = default)
        => _db.StudyPlans
              .Include(p => p.Entries)
              .AsNoTracking()
              .Where(p => _db.SchoolYears.Any(y => y.Id == p.SchoolYearId && y.Year == year))
              .FirstOrDefaultAsync(ct);

    public async Task<Guid> AddAsync(StudyPlan entity, CancellationToken ct = default)
    {
        await _db.StudyPlans.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct); // auto-save
        return entity.Id;
    }

    public async Task UpdateAsync(StudyPlan entity, CancellationToken ct = default)
    {
        _db.StudyPlans.Update(entity);
        await _db.SaveChangesAsync(ct); // auto-save
    }

    public Task<StudyPlanEntry?> FindEntryAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, CancellationToken ct = default)
        => _db.StudyPlanEntries.AsNoTracking()
           .FirstOrDefaultAsync(e => e.StudyPlanId == studyPlanId && e.GradeId == gradeId && e.SubjectId == subjectId, ct);

    public async Task UpsertEntryAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, byte weeklyHours, string? notes, CancellationToken ct = default)
    {
        var entry = await _db.StudyPlanEntries
            .FirstOrDefaultAsync(e => e.StudyPlanId == studyPlanId && e.GradeId == gradeId && e.SubjectId == subjectId, ct);

        if (entry is null)
        {
            entry = new StudyPlanEntry(Guid.NewGuid(), studyPlanId, gradeId, subjectId, weeklyHours, notes);
            await _db.StudyPlanEntries.AddAsync(entry, ct);
        }
        else
        {
            entry.SetWeeklyHours(weeklyHours);
            entry.SetNotes(notes);
            _db.StudyPlanEntries.Update(entry);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveEntryAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, CancellationToken ct = default)
    {
        var entry = await _db.StudyPlanEntries
            .FirstOrDefaultAsync(e => e.StudyPlanId == studyPlanId && e.GradeId == gradeId && e.SubjectId == subjectId, ct);

        if (entry is null) return;
        _db.StudyPlanEntries.Remove(entry);
        await _db.SaveChangesAsync(ct);
    }
}
