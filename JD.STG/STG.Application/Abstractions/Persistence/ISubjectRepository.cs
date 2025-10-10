using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface ISubjectRepository
{
    Task<Subject?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Subject?> GetByNameAsync(Guid schoolYearId, string name, CancellationToken ct = default);
    Task<IReadOnlyList<Subject>> GetBySchoolYearAsync(Guid schoolYearId, CancellationToken ct = default);
    Task AddAsync(Subject entity, CancellationToken ct = default);
}
