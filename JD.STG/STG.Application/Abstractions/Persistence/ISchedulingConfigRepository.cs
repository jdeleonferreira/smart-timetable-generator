using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface ISchedulingConfigRepository
{
    Task<SchedulingConfig?> GetBySchoolYearAsync(Guid schoolYearId, CancellationToken ct = default);
    Task AddAsync(SchedulingConfig entity, CancellationToken ct = default);
}
