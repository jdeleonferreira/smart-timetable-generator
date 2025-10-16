using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ISchoolYearRepository"/>.</summary>
internal sealed class SchoolYearRepository : ISchoolYearRepository
{
    private readonly StgDbContext _db;

    public SchoolYearRepository(StgDbContext db) { _db = db; }

    public async Task<SchoolYear?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.SchoolYears
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<SchoolYear?> GetByYearAsync(int year, CancellationToken ct = default)
        => await _db.SchoolYears
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Year == year, ct);

    public async Task AddAsync(SchoolYear entity, CancellationToken ct = default)
        => await _db.SchoolYears.AddAsync(entity, ct);

    public async Task UpdateAsync(SchoolYear entity, CancellationToken ct = default)
    {
        _db.SchoolYears.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
