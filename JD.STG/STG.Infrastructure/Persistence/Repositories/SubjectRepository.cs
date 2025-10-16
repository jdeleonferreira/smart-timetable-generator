// src/STG.Infrastructure/Persistence/Repositories/SubjectRepository.cs
using Microsoft.EntityFrameworkCore;
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Repositories;

internal sealed class SubjectRepository : ISubjectRepository
{
    private readonly StgDbContext _db;
    public SubjectRepository(StgDbContext db) => _db = db;

    public Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Subject?> GetByNameAsync(string name, CancellationToken ct = default)
        => _db.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Name == name, ct);

    public Task<List<Subject>> ListByStudyAreaAsync(Guid studyAreaId, CancellationToken ct = default)
        => _db.Subjects.AsNoTracking()
            .Where(s => s.StudyAreaId == studyAreaId)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public Task<List<Subject>> ListAllAsync(CancellationToken ct = default)
        => _db.Subjects.AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<Guid> AddAsync(Subject entity, CancellationToken ct = default)
    {
        await _db.Subjects.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct); // auto-save
        return entity.Id;
    }

    public async Task UpdateAsync(Subject entity, CancellationToken ct = default)
    {
        _db.Subjects.Update(entity);
        await _db.SaveChangesAsync(ct); // auto-save
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tracked = await _db.Subjects.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (tracked is null) return;
        _db.Subjects.Remove(tracked);
        await _db.SaveChangesAsync(ct); // auto-save
    }
}
