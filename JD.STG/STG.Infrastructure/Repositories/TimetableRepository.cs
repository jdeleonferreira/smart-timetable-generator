using Microsoft.EntityFrameworkCore;
using STG.Application.Interfaces;
using STG.Domain.Entities;
using STG.Infrastructure.Persistence;

namespace STG.Infrastructure.Repositories;

public class TimetableRepository : ITimetableRepository
{
    private readonly StgDbContext _db;
    public TimetableRepository(StgDbContext db) => _db = db;

    public Task<Timetable?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Timetables.Include(t => t.Assignments).FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Timetable?> GetByYearAsync(int year, CancellationToken ct)
        => _db.Timetables.Include(t => t.Assignments).FirstOrDefaultAsync(t => t.Year == year, ct);

    public Task AddAsync(Timetable timetable, CancellationToken ct)
        => _db.Timetables.AddAsync(timetable, ct).AsTask();

    public void Remove(Timetable timetable) => _db.Timetables.Remove(timetable);
}
