using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface ITimetableEntryRepository
{
    Task<IReadOnlyList<TimetableEntry>> GetByTimetableAsync(Guid timetableId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TimetableEntry> entries, CancellationToken ct = default);
}
