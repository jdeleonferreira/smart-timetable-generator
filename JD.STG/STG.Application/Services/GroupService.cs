using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;

/// <summary>Application service for managing Groups.</summary>
public sealed class GroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGradeRepository _gradeRepository;

    public GroupService(IGroupRepository groups, IGradeRepository grades)
    {
        _groupRepository = groups;
        _gradeRepository = grades;
    }

    /// <summary>Create a Group under a Grade ensuring (GradeId, Name) uniqueness.</summary>
    public async Task<Guid> CreateAsync(Guid gradeId, string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name cannot be empty.", nameof(name));

        // Ensure grade exists
        _ = await _gradeRepository.GetByIdAsync(gradeId, ct) ?? throw new KeyNotFoundException("Grade not found.");

        var dup = await _groupRepository.GetByGradeAndNameAsync(gradeId, name.Trim(), ct);
        if (dup is not null)
            throw new InvalidOperationException($"Group '{name}' already exists for this grade.");

        var entity = new Group(Guid.NewGuid(), gradeId, name.Trim());
        return await _groupRepository.AddAsync(entity, ct);
    }

    /// <summary>Bulk create groups A..N for a grade (skips existing names).</summary>
    public async Task<List<Guid>> CreateBulkAsync(Guid gradeId, IEnumerable<string> names, CancellationToken ct = default)
    {
        _ = await _gradeRepository.GetByIdAsync(gradeId, ct) ?? throw new KeyNotFoundException("Grade not found.");

        var existing = await _groupRepository.ListByGradeAsync(gradeId, ct);
        var existingSet = existing.Select(g => g.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toCreate = names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Where(n => !existingSet.Contains(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(n => new Group(Guid.NewGuid(), gradeId, n))
            .ToList();

        if (toCreate.Count == 0) return new List<Guid>();

        await _groupRepository.AddRangeAsync(toCreate, ct);
        return toCreate.Select(g => g.Id).ToList();
    }

    /// <summary>Rename a Group ensuring (GradeId, Name) uniqueness.</summary>
    public async Task RenameAsync(Guid id, string newName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("New name cannot be empty.", nameof(newName));

        var current = await _groupRepository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Group not found.");

        var dup = await _groupRepository.GetByGradeAndNameAsync(current.GradeId, newName.Trim(), ct);
        if (dup is not null && dup.Id != id)
            throw new InvalidOperationException($"Group '{newName}' already exists for this grade.");

        current.Rename(newName.Trim());
        await _groupRepository.UpdateAsync(current, ct);
    }

    public async Task<List<Group>> ListByGradeAsync(Guid gradeId, CancellationToken ct = default)
        => await _groupRepository.ListByGradeAsync(gradeId, ct);

    public async Task<List<Group>> ListAllAsync(CancellationToken ct = default)
        => await _groupRepository.ListAllAsync(ct);
}
