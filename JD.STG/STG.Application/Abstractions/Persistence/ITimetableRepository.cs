using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

/// <summary>Repository contract for <see cref="Timetable"/> aggregate.</summary>
public interface ITimetableRepository
{
    Task<Timetable?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Timetable?> GetByGroupAndYearAsync(Guid groupId, Guid schoolYearId, CancellationToken ct = default);
    Task<List<Timetable>> ListByYearAsync(Guid schoolYearId, CancellationToken ct = default);

    // Auto-save persistence
    Task<Guid> AddAsync(Timetable entity, CancellationToken ct = default);
    Task UpdateAsync(Timetable entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}