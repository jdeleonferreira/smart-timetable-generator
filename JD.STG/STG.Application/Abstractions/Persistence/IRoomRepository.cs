using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IRoomRepository
{
    Task<Room?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Room>> GetBySchoolYearAsync(Guid schoolYearId, CancellationToken ct = default);
    Task AddAsync(Room entity, CancellationToken ct = default);
}
