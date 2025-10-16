using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

/// <summary>Repository contract for <see cref="StudyPlan"/> aggregate.</summary>
public interface IStudyPlanRepository
{
    Task<StudyPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StudyPlan?> GetByYearAsync(int year, CancellationToken ct = default); // convenience query
    Task<Guid> AddAsync(StudyPlan entity, CancellationToken ct = default);     // auto-save
    Task UpdateAsync(StudyPlan entity, CancellationToken ct = default);        // auto-save

    // Entries (optional helpers if you need direct access)
    Task<StudyPlanEntry?> FindEntryAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, CancellationToken ct = default);
    Task UpsertEntryAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, byte weeklyHours, string? notes, CancellationToken ct = default);
    Task RemoveEntryAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, CancellationToken ct = default);
}
