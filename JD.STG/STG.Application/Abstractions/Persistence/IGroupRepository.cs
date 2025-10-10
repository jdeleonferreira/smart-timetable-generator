using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Group?> GetByCodeAsync(Guid schoolYearId, string gradeName, string label, CancellationToken ct = default);
    Task<IReadOnlyList<Group>> ListByGradeAsync(Guid gradeId, CancellationToken ct = default);

    Task AddAsync(Group entity, CancellationToken ct = default);
    void Update(Group entity);
}