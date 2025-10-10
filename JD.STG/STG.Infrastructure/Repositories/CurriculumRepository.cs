using Microsoft.EntityFrameworkCore;
using STG.Application.Interfaces;
using STG.Domain.Entities;
using STG.Infrastructure.Persistence;

namespace STG.Infrastructure.Repositories;

public class CurriculumRepository : ICurriculumRepository
{
    private readonly StgDbContext _db;
    public CurriculumRepository(StgDbContext db) => _db = db;

    public async Task ClearYearAsync(int year, CancellationToken ct)
    {
        var lines = await _db.CurriculumLines.Where(c => c.Year == year).ToListAsync(ct);
        _db.CurriculumLines.RemoveRange(lines);
    }

    public Task AddRangeAsync(IEnumerable<StudyPlanEntry> items, CancellationToken ct)
        => _db.CurriculumLines.AddRangeAsync(items, ct);

    public async Task<IReadOnlyList<StudyPlanEntry>> GetByYearAsync(int year, CancellationToken ct)
        => await _db.CurriculumLines.Where(c => c.Year == year).AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<StudyPlanEntry>> GetByGradeAsync(int year, string grade, CancellationToken ct)
        => await _db.CurriculumLines.Where(c => c.Year == year && c.Grade == grade).AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<StudyPlanEntry>> GetBySubjectAsync(int year, string subject, CancellationToken ct)
        => await _db.CurriculumLines.Where(c => c.Year == year && c.Subject == subject).AsNoTracking().ToListAsync(ct);
}
