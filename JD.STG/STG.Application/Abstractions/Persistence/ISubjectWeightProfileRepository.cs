using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface ISubjectWeightProfileRepository
{
    Task<IReadOnlyList<SubjectWeightProfile>> GetBySubjectAsync(Guid subjectId, CancellationToken ct = default);
    Task<SubjectWeightProfile?> GetForSubjectAndGradeAsync(Guid subjectId, Guid? gradeId, CancellationToken ct = default);
    Task AddAsync(SubjectWeightProfile entity, CancellationToken ct = default);
}