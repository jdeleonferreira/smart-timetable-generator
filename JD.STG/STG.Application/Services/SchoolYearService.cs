using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;

/// <summary>
/// Application service that manages academic years (no CQRS/MediatR).
/// </summary>
public sealed class SchoolYearService
{
    private readonly ISchoolYearRepository _schoolYearRepository;

    public SchoolYearService(ISchoolYearRepository repo)
    {
        _schoolYearRepository = repo;
    }

    /// <summary>
    /// Ensures a SchoolYear exists for the given year and returns it.
    /// If it does not exist, it is created and persisted.
    /// </summary>
    public async Task<SchoolYear> EnsureAsync(int year, CancellationToken ct = default)
    {
        var existing = await _schoolYearRepository.GetByYearAsync(year, ct);
        if (existing is not null) return existing;

        var entity = new SchoolYear(year);
        await _schoolYearRepository.AddAsync(entity, ct);

        return entity;
    }

    /// <summary>Creates a new SchoolYear; throws if the year already exists.</summary>
    /// <exception cref="InvalidOperationException">If duplicate year.</exception>
    public async Task<Guid> CreateAsync(int year, CancellationToken ct = default)
    {
        if (await _schoolYearRepository.GetByYearAsync(year, ct) is not null)
            throw new InvalidOperationException($"SchoolYear {year} already exists.");

        var entity = new SchoolYear(year);
        await _schoolYearRepository.AddAsync(entity, ct);

        return entity.Id;
    }

    /// <summary>Updates the year value (rarely used) respecting range and uniqueness.</summary>
    public async Task RenameYearAsync(Guid id, int newYear, CancellationToken ct = default)
    {
        var dup = await _schoolYearRepository.GetByYearAsync(newYear, ct);
        if (dup is not null && dup.Id != id)
            throw new InvalidOperationException($"SchoolYear {newYear} already exists.");

        // load tracked entity to update
        var current = await _schoolYearRepository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("SchoolYear not found.");
        current.SetYear(newYear);

        await _schoolYearRepository.UpdateAsync(current, ct);
    }
}
