using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;

/// <summary>Application service for managing Subjects.</summary>
public sealed class SubjectService
{
    private readonly ISubjectRepository _subjects;
    private readonly IStudyAreaRepository _areas;

    public SubjectService(ISubjectRepository subjects, IStudyAreaRepository areas)
    {
        _subjects = subjects;
        _areas = areas;
    }

    /// <summary>Create a new Subject ensuring name uniqueness and valid StudyArea.</summary>
    public async Task<Guid> CreateAsync(string name, Guid studyAreaId, string? code = null, bool isElective = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Subject name cannot be empty.", nameof(name));

        // Ensure StudyArea exists
        _ = await _areas.GetByIdAsync(studyAreaId, ct) ?? throw new KeyNotFoundException("StudyArea not found.");

        // Uniqueness by name (institution-wide). If you prefer per StudyArea, change this rule.
        var dup = await _subjects.GetByNameAsync(name.Trim(), ct);
        if (dup is not null)
            throw new InvalidOperationException($"Subject '{name}' already exists.");

        var entity = new Subject(Guid.NewGuid(), name.Trim(), studyAreaId, code?.Trim(), isElective);
        return await _subjects.AddAsync(entity, ct);
    }

    /// <summary>Rename an existing Subject (preserves uniqueness).</summary>
    public async Task RenameAsync(Guid id, string newName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("New name cannot be empty.", nameof(newName));

        var dup = await _subjects.GetByNameAsync(newName.Trim(), ct);
        if (dup is not null && dup.Id != id)
            throw new InvalidOperationException($"Subject '{newName}' already exists.");

        var current = await _subjects.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Subject not found.");
        current.Rename(newName.Trim());
        await _subjects.UpdateAsync(current, ct);
    }

    /// <summary>Move a Subject to another StudyArea.</summary>
    public async Task MoveToAreaAsync(Guid id, Guid newStudyAreaId, CancellationToken ct = default)
    {
        // Ensure new area exists
        _ = await _areas.GetByIdAsync(newStudyAreaId, ct) ?? throw new KeyNotFoundException("StudyArea not found.");

        var current = await _subjects.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Subject not found.");
        current.SetStudyArea(newStudyAreaId);
        await _subjects.UpdateAsync(current, ct);
    }

    public Task<List<Subject>> ListAllAsync(CancellationToken ct = default) => _subjects.ListAllAsync(ct);
    public Task<List<Subject>> ListByAreaAsync(Guid studyAreaId, CancellationToken ct = default) => _subjects.ListByStudyAreaAsync(studyAreaId, ct);
}
