using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;

/// <summary>Application service for managing StudyAreas.</summary>
public sealed class StudyAreaService
{
    private readonly IStudyAreaRepository _studyAreasRepository;

    public StudyAreaService(IStudyAreaRepository areas) => _studyAreasRepository = areas;

    /// <summary>Creates a new StudyArea ensuring name uniqueness.</summary>
    /// <exception cref="InvalidOperationException">If the name already exists.</exception>
    public async Task<Guid> CreateAsync(string name, string? code, byte orderNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("StudyArea name cannot be empty.", nameof(name));

        var exists = await _studyAreasRepository.GetByNameAsync(name.Trim(), ct);
        if (exists is not null)
            throw new InvalidOperationException($"StudyArea '{name}' already exists.");

        var entity = new StudyArea(Guid.NewGuid(), name.Trim(), code?.Trim(), orderNo, isActive: true);
        return await _studyAreasRepository.AddAsync(entity, ct); // repo persists
    }

    /// <summary>Renames an existing StudyArea while preserving uniqueness.</summary>
    public async Task RenameAsync(Guid id, string newName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("New name cannot be empty.", nameof(newName));

        var dup = await _studyAreasRepository.GetByNameAsync(newName.Trim(), ct);
        if (dup is not null && dup.Id != id)
            throw new InvalidOperationException($"StudyArea '{newName}' already exists.");

        var current = await _studyAreasRepository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("StudyArea not found.");
        current.Rename(newName.Trim());
        await _studyAreasRepository.UpdateAsync(current, ct);
    }

    /// <summary>Deactivates a StudyArea (soft off).</summary>
    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var current = await _studyAreasRepository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("StudyArea not found.");
        current.Deactivate();
        await _studyAreasRepository.UpdateAsync(current, ct);
    }

    /// <summary>Returns the ordered list of StudyAreas for selection UIs.</summary>
    public Task<List<StudyArea>> ListAsync(CancellationToken ct = default) => _studyAreasRepository.ListAsync(ct);
}
