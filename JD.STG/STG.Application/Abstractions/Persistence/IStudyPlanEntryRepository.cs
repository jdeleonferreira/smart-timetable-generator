using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

/// <summary>Repository contract for <see cref="StudyPlanEntry"/>.</summary>
public interface IStudyPlanEntryRepository
{
    Task<StudyPlanEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StudyPlanEntry?> FindAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, CancellationToken ct = default);
    Task<List<StudyPlanEntry>> ListByPlanAsync(Guid studyPlanId, CancellationToken ct = default);
    Task<List<StudyPlanEntry>> ListByPlanAndGradeAsync(Guid studyPlanId, Guid gradeId, CancellationToken ct = default);

    // Auto-save persistence methods
    Task<Guid> AddAsync(StudyPlanEntry entity, CancellationToken ct = default);
    Task UpdateAsync(StudyPlanEntry entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task DeleteByKeyAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, CancellationToken ct = default);
}
