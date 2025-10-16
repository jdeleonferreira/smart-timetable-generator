using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

/// <summary>Repository contract for <see cref="SchoolYear"/>.</summary>
public interface ISchoolYearRepository
{
    Task<SchoolYear?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SchoolYear?> GetByYearAsync(int year, CancellationToken ct = default);
    Task AddAsync(SchoolYear entity, CancellationToken ct = default);
    Task UpdateAsync(SchoolYear entity, CancellationToken ct = default);
}