using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IAssignmentRepository
{
    Task<Assignment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsForGroupSubjectAsync(Guid schoolYearId, Guid groupId, Guid subjectId, CancellationToken ct = default);
    Task<IReadOnlyList<Assignment>> ListByGroupAsync(Guid schoolYearId, Guid groupId, CancellationToken ct = default);
    Task<IReadOnlyList<Assignment>> ListByTeacherAsync(Guid schoolYearId, Guid teacherId, CancellationToken ct = default);

    Task AddAsync(Assignment entity, CancellationToken ct = default);
    void Update(Assignment entity);
    void Remove(Assignment entity);
}
