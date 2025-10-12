using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

/// <summary>Repository contract for <see cref="Assignment"/>.</summary>
public interface IAssignmentRepository
{
    Task<Assignment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Assignment?> GetAsync(Guid groupId, Guid subjectId, Guid schoolYearId, CancellationToken ct = default);

    Task<List<Assignment>> ListByGroupAsync(Guid groupId, Guid schoolYearId, CancellationToken ct = default);
    Task<List<Assignment>> ListByTeacherAsync(Guid teacherId, Guid schoolYearId, CancellationToken ct = default);
    Task<List<Assignment>> ListByYearAsync(Guid schoolYearId, CancellationToken ct = default);

    // Auto-save persistence methods
    Task<Guid> AddAsync(Assignment entity, CancellationToken ct = default);
    Task UpsertAsync(Guid groupId, Guid subjectId, Guid schoolYearId, byte weeklyHours, Guid? teacherId, string? notes, CancellationToken ct = default);
    Task UpdateAsync(Assignment entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
