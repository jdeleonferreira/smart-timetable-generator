using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

/// <summary>
/// CRUD-style access for Teacher entities, plus simple queries by qualification.
/// </summary>
public interface ITeacherRepository
{
    Task<Teacher?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Teacher?> GetByNameAsync(string name, CancellationToken ct = default);

    Task<IReadOnlyList<Teacher>> ListAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Teacher>> ListQualifiedForAsync(string subjectName, CancellationToken ct = default);

    Task AddAsync(Teacher entity, CancellationToken ct = default);
    void Update(Teacher entity);
    void Remove(Teacher entity);
}