using STG.Domain.Entities;

namespace STG.Application.Interfaces;

public interface ISubjectRepository
{
    Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Subject?> GetByNameAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken ct);
    Task AddAsync(Subject subject, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Subject> subjects, CancellationToken ct);
    void Remove(Subject subject);
}
