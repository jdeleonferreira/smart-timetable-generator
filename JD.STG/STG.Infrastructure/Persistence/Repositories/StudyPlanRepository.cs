using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

public sealed class StudyPlanRepository : IStudyPlanRepository
{
    private readonly StgDbContext _db;
    public StudyPlanRepository(StgDbContext db) => _db = db;

    public async Task<byte?> GetHoursAsync(Guid schoolYearId, Guid gradeId, Guid subjectId, CancellationToken ct = default)
    {
        var entry = await _db.StudyPlanEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolYearId == schoolYearId && x.GradeId == gradeId && x.SubjectId == subjectId, ct);

        return entry?.WeeklyHours;
    }

    public async Task<IReadOnlyList<StudyPlanEntry>> ListByGradeAsync(Guid schoolYearId, Guid gradeId, CancellationToken ct = default) =>
        await _db.StudyPlanEntries
            .Where(x => x.SchoolYearId == schoolYearId && x.GradeId == gradeId)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task AddAsync(StudyPlanEntry entry, CancellationToken ct = default)
    {
        _db.StudyPlanEntries.Add(entry);
        return Task.CompletedTask;
    }

    public async Task UpsertAsync(StudyPlanEntry entry, CancellationToken ct = default)
    {
        var existing = await _db.StudyPlanEntries.FirstOrDefaultAsync(x =>
            x.SchoolYearId == entry.SchoolYearId &&
            x.GradeId == entry.GradeId &&
            x.SubjectId == entry.SubjectId, ct);

        if (existing is null)
        {
            _db.StudyPlanEntries.Add(entry);
        }
        else
        {
            existing = new StudyPlanEntry(entry.SchoolYearId, entry.GradeId, entry.SubjectId, entry.WeeklyHours) { };
            _db.StudyPlanEntries.Update(existing);
        }
    }
}
