using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IPeriodSlotRepository
{
    Task<IReadOnlyList<PeriodSlot>> GetBySchoolYearAsync(Guid schoolYearId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<PeriodSlot> slots, CancellationToken ct = default);
}