using STG.Domain.Entities;

namespace STG.Application.Interfaces;

public interface ITimetableRepository
{
    Task<Timetable?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Timetable?> GetByYearAsync(int year, CancellationToken ct);
    Task AddAsync(Timetable timetable, CancellationToken ct);
    void Remove(Timetable timetable);
}
