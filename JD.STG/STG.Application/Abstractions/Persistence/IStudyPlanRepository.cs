using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface IStudyPlanRepository
{
    Task<byte?> GetHoursAsync(Guid schoolYearId, Guid gradeId, Guid subjectId, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlanEntry>> ListByGradeAsync(Guid schoolYearId, Guid gradeId, CancellationToken ct = default);

    Task AddAsync(StudyPlanEntry entry, CancellationToken ct = default);
    Task UpsertAsync(StudyPlanEntry entry, CancellationToken ct = default);
}
