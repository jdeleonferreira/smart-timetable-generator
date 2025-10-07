using STG.Domain.Entities;

namespace STG.Application.Interfaces;

public interface ICurriculumRepository
{
    Task ClearYearAsync(int year, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<CurriculumLine> items, CancellationToken ct);
    Task<IReadOnlyList<CurriculumLine>> GetByYearAsync(int year, CancellationToken ct);
    Task<IReadOnlyList<CurriculumLine>> GetByGradeAsync(int year, string grade, CancellationToken ct);
    Task<IReadOnlyList<CurriculumLine>> GetBySubjectAsync(int year, string subject, CancellationToken ct);
}
