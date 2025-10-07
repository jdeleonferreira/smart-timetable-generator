using STG.Application.Interfaces;
using STG.Infrastructure.Persistence;

namespace STG.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly StgDbContext _db;
    public UnitOfWork(StgDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
