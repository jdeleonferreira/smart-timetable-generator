using STG.Domain.Entities;
using STG.Domain.ValueObjects;

namespace STG.Application.Abstractions.Persistence;

public interface ITimetableRepository
{
    Task<Timetable> GetOrCreateForYearAsync(Guid schoolYearId, CancellationToken ct = default);

    Task<IReadOnlyList<TimetableEntry>> ListEntriesByGroupAsync(Guid schoolYearId, Guid groupId, CancellationToken ct = default);
    Task<IReadOnlyList<TimetableEntry>> ListEntriesByTeacherAsync(Guid schoolYearId, Guid teacherId, CancellationToken ct = default);

    Task AddEntryAsync(TimetableEntry entry, CancellationToken ct = default);
    Task RemoveEntryAsync(Guid entryId, CancellationToken ct = default);

    Task<bool> IsGroupTakenAsync(Guid schoolYearId, Guid groupId, TimeSlot slot, CancellationToken ct = default);
    Task<bool> IsTeacherTakenAsync(Guid schoolYearId, Guid teacherId, TimeSlot slot, CancellationToken ct = default);
}