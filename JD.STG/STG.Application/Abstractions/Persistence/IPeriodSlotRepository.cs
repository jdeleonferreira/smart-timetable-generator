using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence
{
    public interface IPeriodSlotRepository
    {
        Task<PeriodSlot?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<PeriodSlot>> ListBySchoolYearAsync(Guid schoolYearId, CancellationToken ct = default);
        Task<IReadOnlyList<PeriodSlot>> ListBySchoolYearAndDayAsync(Guid schoolYearId, int dayOfWeek, CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid schoolYearId, int dayOfWeek, int periodNumber, CancellationToken ct = default);

        Task AddAsync(PeriodSlot slot, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<PeriodSlot> slots, CancellationToken ct = default);
        Task UpdateAsync(PeriodSlot slot, CancellationToken ct = default);
        Task DeleteAsync(PeriodSlot slot, CancellationToken ct = default);
    }
}
