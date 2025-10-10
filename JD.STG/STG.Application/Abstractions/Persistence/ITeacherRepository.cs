// FILE: STG.Application/Abstractions/Persistence/Repositories/ITeacherRepository.cs
using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

public interface ITeacherRepository
{
    Task<Teacher?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Teacher>> GetBySchoolYearAsync(Guid schoolYearId, CancellationToken ct = default);
    Task AddAsync(Teacher entity, CancellationToken ct = default);
}