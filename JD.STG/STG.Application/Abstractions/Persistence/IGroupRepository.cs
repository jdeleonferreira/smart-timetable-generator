using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IGroupRepository
{
    Task<Group?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Group>> GetByGradeAsync(Guid gradeId, CancellationToken ct = default);
    Task AddAsync(Group entity, CancellationToken ct = default);
}
