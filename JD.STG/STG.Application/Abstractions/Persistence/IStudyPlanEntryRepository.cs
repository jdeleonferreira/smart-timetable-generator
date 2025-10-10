using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IStudyPlanEntryRepository
{
    Task<IReadOnlyList<StudyPlanEntry>> GetByPlanAsync(Guid studyPlanId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<StudyPlanEntry> entries, CancellationToken ct = default);
}