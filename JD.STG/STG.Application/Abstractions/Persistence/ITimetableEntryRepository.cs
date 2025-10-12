using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

/// <summary>Repository contract for <see cref="TimetableEntry"/>.</summary>
public interface ITimetableEntryRepository
{
    Task<TimetableEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TimetableEntry?> FindAsync(Guid timetableId, byte dayOfWeek, byte periodIndex, CancellationToken ct = default);
    Task<List<TimetableEntry>> ListByTimetableAsync(Guid timetableId, CancellationToken ct = default);

    // Auto-save persistence
    Task<Guid> AddAsync(TimetableEntry entity, CancellationToken ct = default);
    Task UpdateAsync(TimetableEntry entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}