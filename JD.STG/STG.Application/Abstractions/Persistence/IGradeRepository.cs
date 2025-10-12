using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

/// <summary>Repository contract for <see cref="Grade"/>.</summary>
public interface IGradeRepository
{
    Task<Grade?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Grade?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Grade?> GetByOrderAsync(byte order, CancellationToken ct = default);
    Task<List<Grade>> ListAsync(CancellationToken ct = default);

    // Auto-save persistence methods
    Task<Guid> AddAsync(Grade entity, CancellationToken ct = default);
    Task UpdateAsync(Grade entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
