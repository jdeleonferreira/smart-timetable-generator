using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IAssignmentRepository
{
    Task<Assignment?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Assignment>> GetBySchoolYearAsync(Guid schoolYearId, CancellationToken ct = default);
    Task<IReadOnlyList<Assignment>> GetByGroupAsync(Guid groupId, CancellationToken ct = default);
    Task AddAsync(Assignment entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Assignment> entities, CancellationToken ct = default);
}
