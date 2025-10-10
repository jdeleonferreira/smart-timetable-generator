using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

/// <summary>
/// CRUD-style access for Subject entities.
/// </summary>
public interface ISubjectRepository
{
    Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Subject?> GetByNameAsync(string name, CancellationToken ct = default);

    Task<IReadOnlyList<Subject>> ListAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Subject>> SearchByPrefixAsync(string prefix, int max = 20, CancellationToken ct = default);

    Task AddAsync(Subject entity, CancellationToken ct = default);
    void Update(Subject entity);
    void Remove(Subject entity);
}
