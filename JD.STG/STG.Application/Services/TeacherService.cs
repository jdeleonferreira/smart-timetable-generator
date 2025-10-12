// src/STG.Application/Personnel/TeacherService.cs
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;

/// <summary>Application service for managing Teachers (no CQRS/MediatR).</summary>
public sealed class TeacherService
{
    private readonly ITeacherRepository _teacherRepository;

    public TeacherService(ITeacherRepository teachers) => _teacherRepository = teachers;

    /// <summary>Create a teacher ensuring email uniqueness when provided.</summary>
    public async Task<Guid> CreateAsync(string fullName, string? email = null, byte? maxWeeklyLoad = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty.", nameof(fullName));

        if (!string.IsNullOrWhiteSpace(email))
        {
            var dup = await _teacherRepository.GetByEmailAsync(email.Trim(), ct);
            if (dup is not null)
                throw new InvalidOperationException($"Email '{email}' is already in use.");
        }

        var entity = new Teacher(Guid.NewGuid(), fullName.Trim(), string.IsNullOrWhiteSpace(email) ? null : email!.Trim(), maxWeeklyLoad, isActive: true);
        return await _teacherRepository.AddAsync(entity, ct);
    }

    public async Task RenameAsync(Guid id, string fullName, CancellationToken ct = default)
    {
        var current = await _teacherRepository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Teacher not found.");
        current.Rename(fullName);
        await _teacherRepository.UpdateAsync(current, ct);
    }

    public async Task SetEmailAsync(Guid id, string? email, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var dup = await _teacherRepository.GetByEmailAsync(email.Trim(), ct);
            if (dup is not null && dup.Id != id)
                throw new InvalidOperationException($"Email '{email}' is already in use.");
        }

        var current = await _teacherRepository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Teacher not found.");
        current.SetEmail(email);
        await _teacherRepository.UpdateAsync(current, ct);
    }

    public async Task SetMaxWeeklyLoadAsync(Guid id, byte? hours, CancellationToken ct = default)
    {
        var current = await _teacherRepository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Teacher not found.");
        current.SetMaxWeeklyLoad(hours);
        await _teacherRepository.UpdateAsync(current, ct);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var current = await _teacherRepository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Teacher not found.");
        current.Deactivate();
        await _teacherRepository.UpdateAsync(current, ct);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var current = await _teacherRepository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Teacher not found.");
        current.Activate();
        await _teacherRepository.UpdateAsync(current, ct);
    }

    public Task<List<Teacher>> ListAsync(bool onlyActive = true, CancellationToken ct = default)
        => _teacherRepository.ListAllAsync(onlyActive, ct);
}
