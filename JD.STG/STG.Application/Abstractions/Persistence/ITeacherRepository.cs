// FILE: STG.Application/Abstractions/Persistence/Repositories/ITeacherRepository.cs
using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

/// <summary>Repository contract for <see cref="Teacher"/>.</summary>
public interface ITeacherRepository
{
    Task<Teacher?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Teacher?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<List<Teacher>> ListAllAsync(bool onlyActive, CancellationToken ct = default);

    // Auto-save persistence methods
    Task<Guid> AddAsync(Teacher entity, CancellationToken ct = default);
    Task UpdateAsync(Teacher entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}