using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IAvailabilityBlockRepository
{
    Task<IReadOnlyList<AvailabilityBlock>> GetByTeacherAsync(Guid teacherId, CancellationToken ct = default);
    Task<IReadOnlyList<AvailabilityBlock>> GetBySchoolYearAsync(Guid schoolYearId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<AvailabilityBlock> blocks, CancellationToken ct = default);
}
