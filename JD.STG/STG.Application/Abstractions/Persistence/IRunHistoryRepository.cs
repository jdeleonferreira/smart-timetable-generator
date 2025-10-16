using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IRunHistoryRepository
{
    Task AddAsync(RunHistory entity, CancellationToken ct = default);
    Task<RunHistory?> GetAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(RunHistory entity, CancellationToken ct = default);
}
