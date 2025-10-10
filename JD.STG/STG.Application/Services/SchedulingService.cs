using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;
using STG.Domain.ValueObjects;

namespace STG.Application.Services;

public sealed class SchedulingService
{
    private readonly ITimetableRepository _ttRepo;
    private readonly IAssignmentRepository _assignments;
    private readonly IUnitOfWork _uow;

    public SchedulingService(ITimetableRepository ttRepo, IAssignmentRepository assignments, IUnitOfWork uow)
    {
        _ttRepo = ttRepo;
        _assignments = assignments;
        _uow = uow;
    }

    // Pone 1 bloque de un assignment en un slot si no rompe reglas duras
    public async Task<Guid> PlaceAsync(Guid schoolYearId, Guid assignmentId, DayOfWeek day, byte block, CancellationToken ct = default)
    {
        var a = await _assignments.GetByIdAsync(assignmentId, ct) ?? throw new KeyNotFoundException("Assignment not found.");
        var slot = new TimeSlot(day, block);
        var tt = await _ttRepo.GetOrCreateForYearAsync(schoolYearId, ct);

        // Reglas duras antes de persistir
        if (await _ttRepo.IsGroupTakenAsync(schoolYearId, a.GroupId, slot, ct))
            throw new InvalidOperationException("Group period already assigned.");
        if (await _ttRepo.IsTeacherTakenAsync(schoolYearId, a.TeacherId, slot, ct))
            throw new InvalidOperationException("Teacher overlap.");

        var entry = new TimetableEntry(tt.Id, schoolYearId, a.Id, a.GroupId, a.SubjectId, a.TeacherId, slot);
        await _ttRepo.AddEntryAsync(entry, ct);

        await _uow.SaveChangesAsync(ct);
        return entry.Id;
    }

    // Elimina una colocación
    public async Task RemovePlacementAsync(Guid entryId, CancellationToken ct = default)
    {
        await _ttRepo.RemoveEntryAsync(entryId, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
