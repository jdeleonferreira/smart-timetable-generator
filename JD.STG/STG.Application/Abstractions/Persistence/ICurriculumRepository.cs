using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface ICurriculumRepository
{
    Task ClearYearAsync(int year, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<StudyPlanEntry> items, CancellationToken ct);
    Task<IReadOnlyList<StudyPlanEntry>> GetByYearAsync(int year, CancellationToken ct);
    Task<IReadOnlyList<StudyPlanEntry>> GetByGradeAsync(int year, string grade, CancellationToken ct);
    Task<IReadOnlyList<StudyPlanEntry>> GetBySubjectAsync(int year, string subject, CancellationToken ct);
}
