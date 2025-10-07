using Microsoft.EntityFrameworkCore;
using STG.Application.Interfaces;
using STG.Domain.Entities;
using STG.Infrastructure.Persistence;

namespace STG.Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly StgDbContext _db;
    public RoomRepository(StgDbContext db) => _db = db;

    public async Task<Room?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _db.Rooms.FindAsync(new object[] { id }, ct).AsTask();

    public async Task<Room?> GetByNameAsync(string name, CancellationToken ct)
        => await _db.Rooms.FirstOrDefaultAsync(r => r.Name == name, ct);

    public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct)
        => await _db.Rooms.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Room>> GetWithTagAsync(string tag, CancellationToken ct)
        => await _db.Rooms.AsNoTracking()
            .Where(r => r.Tags != null && r.Tags.Contains(tag))
            .ToListAsync(ct);

    public async Task AddAsync(Room room, CancellationToken ct)
        => await _db.Rooms.AddAsync(room, ct).AsTask();

    public async Task AddRangeAsync(IEnumerable<Room> rooms, CancellationToken ct)
        => await _db.Rooms.AddRangeAsync(rooms, ct);

    public void Remove(Room room) => _db.Rooms.Remove(room);
}
