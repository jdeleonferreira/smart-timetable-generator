using STG.Domain.Entities;


namespace STG.Application.Abstractions.Persistence;

/// <summary>
/// Repository contract for the Area aggregate.
/// </summary>
public interface IStudyAreaRepository
{
    Task<StudyArea?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StudyArea?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<List<StudyArea>> ListAsync(CancellationToken ct = default);

    Task<Guid> AddAsync(StudyArea entity, CancellationToken ct = default);
    Task UpdateAsync(StudyArea entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
