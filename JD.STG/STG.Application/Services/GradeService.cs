using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;

/// <summary>Application service for managing Grades (no CQRS/MediatR).</summary>
public sealed class GradeService
{
    private readonly IGradeRepository _grades;

    public GradeService(IGradeRepository grades) => _grades = grades;

    /// <summary>Create a new Grade ensuring unique name and order.</summary>
    public async Task<Guid> CreateAsync(string name, byte order, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Grade name cannot be empty.", nameof(name));

        var dupName = await _grades.GetByNameAsync(name.Trim(), ct);
        if (dupName is not null)
            throw new InvalidOperationException($"Grade '{name}' already exists.");

        var dupOrder = await _grades.GetByOrderAsync(order, ct);
        if (dupOrder is not null)
            throw new InvalidOperationException($"A grade with order {order} already exists.");

        var entity = new Grade(Guid.NewGuid(), name.Trim(), order);
        return await _grades.AddAsync(entity, ct);
    }

    /// <summary>Rename a Grade, preserving name uniqueness.</summary>
    public async Task RenameAsync(Guid id, string newName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("New name cannot be empty.", nameof(newName));

        var dup = await _grades.GetByNameAsync(newName.Trim(), ct);
        if (dup is not null && dup.Id != id)
            throw new InvalidOperationException($"Grade '{newName}' already exists.");

        var current = await _grades.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Grade not found.");
        current.Rename(newName.Trim());
        await _grades.UpdateAsync(current, ct);
    }

    /// <summary>Change the order of a Grade, preserving order uniqueness.</summary>
    public async Task ReorderAsync(Guid id, byte newOrder, CancellationToken ct = default)
    {
        var dup = await _grades.GetByOrderAsync(newOrder, ct);
        if (dup is not null && dup.Id != id)
            throw new InvalidOperationException($"A grade with order {newOrder} already exists.");

        var current = await _grades.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Grade not found.");
        current.Reorder(newOrder);
        await _grades.UpdateAsync(current, ct);
    }

    public Task<List<Grade>> ListAsync(CancellationToken ct = default) => _grades.ListAsync(ct);
}
