using STG.Domain.Entities;

namespace STG.Application.Interfaces;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Group?> GetByCodeAsync(string grade, string label, CancellationToken ct); // p.ej. "6"+"A"
    Task<IReadOnlyList<Group>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<Group>> GetByGradeAsync(string grade, CancellationToken ct);
    Task AddAsync(Group group, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Group> groups, CancellationToken ct);
    void Remove(Group group);
}
