using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface ITimetableRepository
{
    Task<Timetable?> GetForGroupAsync(Guid schoolYearId, Guid groupId, CancellationToken ct = default);
    Task AddAsync(Timetable entity, CancellationToken ct = default);
}
