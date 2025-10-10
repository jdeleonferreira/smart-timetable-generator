using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IStudyPlanRepository
{
    Task<StudyPlan?> GetAsync(Guid id, CancellationToken ct = default);
    Task<StudyPlan?> GetByYearAndGradeAsync(Guid schoolYearId, Guid gradeId, CancellationToken ct = default);
    Task AddAsync(StudyPlan entity, CancellationToken ct = default);
}
