using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;
public class PeriodSlotService
{
    private readonly IPeriodSlotRepository _periodSlotRepository;

    public PeriodSlotService(IPeriodSlotRepository repo) => _periodSlotRepository = repo;

    public Task<IReadOnlyList<PeriodSlot>> ListAsync(Guid schoolYearId, CancellationToken ct = default)
        => _periodSlotRepository.ListBySchoolYearAsync(schoolYearId, ct);


    public async Task<Guid> CreateAsync(
        Guid schoolYearId,
        int dayOfWeek,
        int periodNumber,
        TimeOnly startTime,
        TimeOnly endTime,
        bool isBreak,
        string? label,
        CancellationToken ct = default)
    {
        if (await _periodSlotRepository.ExistsAsync(schoolYearId, dayOfWeek, periodNumber, ct))
            throw new InvalidOperationException("Ya existe un PeriodSlot para ese día y periodo.");

        var slot = PeriodSlot.Create(
            schoolYearId,
            dayOfWeek,
            periodNumber,
            startTime,
            endTime,
            isBreak,
            label);

        await _periodSlotRepository.AddAsync(slot, ct);
        return slot.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        TimeOnly startTime,
        TimeOnly endTime,
        bool isBreak,
        string? label,
        CancellationToken ct = default)
    {
        var slot = await _periodSlotRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("PeriodSlot no encontrado.");

        slot.SetTimeRange(startTime, endTime);
        if (isBreak) slot.MarkAsBreak(label);
        else { slot.UnmarkBreak(); slot.SetLabel(label); }

        await _periodSlotRepository.UpdateAsync(slot, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var slot = await _periodSlotRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("PeriodSlot no encontrado.");
        await _periodSlotRepository.DeleteAsync(slot, ct);
    }
}
