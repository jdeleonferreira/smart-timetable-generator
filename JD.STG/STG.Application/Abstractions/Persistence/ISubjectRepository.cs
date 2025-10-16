using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;


/// <summary>Repository contract for the <see cref="Subject"/> aggregate.</summary>
public interface ISubjectRepository
{
    Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Subject?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<List<Subject>> ListByStudyAreaAsync(Guid studyAreaId, CancellationToken ct = default);
    Task<List<Subject>> ListAllAsync(CancellationToken ct = default);

    // Auto-save persistence methods
    Task<Guid> AddAsync(Subject entity, CancellationToken ct = default);
    Task UpdateAsync(Subject entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}