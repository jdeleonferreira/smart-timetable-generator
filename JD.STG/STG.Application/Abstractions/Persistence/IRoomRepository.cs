using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Room?> GetByNameAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<Room>> GetWithTagAsync(string tag, CancellationToken ct);
    Task AddAsync(Room room, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Room> rooms, CancellationToken ct);
    void Remove(Room room);
}
