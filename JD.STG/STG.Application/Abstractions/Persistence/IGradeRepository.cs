using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IGradeRepository
{
    Task<Grade?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Grade>> GetBySchoolYearAsync(Guid schoolYearId, CancellationToken ct = default);
    Task AddAsync(Grade entity, CancellationToken ct = default);
}
