using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

/// <summary>Repository contract for <see cref="Group"/>.</summary>
public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Group?> GetByGradeAndNameAsync(Guid gradeId, string name, CancellationToken ct = default);
    Task<List<Group>> ListByGradeAsync(Guid gradeId, CancellationToken ct = default);
    Task<List<Group>> ListAllAsync(CancellationToken ct = default);


    Task<Guid> AddAsync(Group entity, CancellationToken ct = default);
    Task UpdateAsync(Group entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Group> entities, CancellationToken ct = default);
}