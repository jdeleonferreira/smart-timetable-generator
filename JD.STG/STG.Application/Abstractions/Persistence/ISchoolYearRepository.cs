using STG.Domain.Entities;

namespace STG.Application.Abstractions.Persistence;

/// <summary>Read/write access to SchoolYear aggregate.</summary>
public interface ISchoolYearRepository
{
    Task<SchoolYear?> GetByYearAsync(int year, CancellationToken ct = default);
    Task AddAsync(SchoolYear entity, CancellationToken ct = default);
}
