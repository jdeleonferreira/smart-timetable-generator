using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;

/// <summary>Application service for managing Assignments.</summary>
public sealed class AssignmentService
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISchoolYearRepository _schoolYearRepository;

    public AssignmentService(
        IAssignmentRepository assignments,
        IGroupRepository groups,
        ISubjectRepository subjects,
        ISchoolYearRepository years)
    {
        _assignmentRepository = assignments;
        _groupRepository = groups;
        _subjectRepository = subjects;
        _schoolYearRepository = years;
    }

    /// <summary>Create or update an Assignment idempotently.</summary>
    public async Task UpsertAsync(Guid groupId, Guid subjectId, int year, byte weeklyHours, Guid? teacherId = null, string? notes = null, CancellationToken ct = default)
    {
        // Validate catalogs
        _ = await _groupRepository.GetByIdAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found.");
        _ = await _subjectRepository.GetByIdAsync(subjectId, ct) ?? throw new KeyNotFoundException("Subject not found.");
        var schoolYear = await _schoolYearRepository.GetByYearAsync(year, ct) ?? throw new KeyNotFoundException($"SchoolYear {year} not found.");

        await _assignmentRepository.UpsertAsync(groupId, subjectId, schoolYear.Id, weeklyHours, teacherId, notes, ct);
    }

    /// <summary>Assigns/changes the Teacher for an Assignment.</summary>
    public async Task SetTeacherAsync(Guid groupId, Guid subjectId, int year, Guid? teacherId, CancellationToken ct = default)
    {
        var schoolYear = await _schoolYearRepository.GetByYearAsync(year, ct) ?? throw new KeyNotFoundException($"SchoolYear {year} not found.");
        var current = await _assignmentRepository.GetAsync(groupId, subjectId, schoolYear.Id, ct)
                      ?? throw new KeyNotFoundException("Assignment not found.");

        current.SetTeacher(teacherId);
        await _assignmentRepository.UpdateAsync(current, ct);
    }

    /// <summary>Updates weekly hours.</summary>
    public async Task SetWeeklyHoursAsync(Guid groupId, Guid subjectId, int year, byte weeklyHours, CancellationToken ct = default)
    {
        var schoolYear = await _schoolYearRepository.GetByYearAsync(year, ct) ?? throw new KeyNotFoundException($"SchoolYear {year} not found.");
        var current = await _assignmentRepository.GetAsync(groupId, subjectId, schoolYear.Id, ct)
                      ?? throw new KeyNotFoundException("Assignment not found.");

        current.SetWeeklyHours(weeklyHours);
        await _assignmentRepository.UpdateAsync(current, ct);
    }

    public async Task RemoveAsync(Guid groupId, Guid subjectId, int year, CancellationToken ct = default)
    {
        var schoolYear = await _schoolYearRepository.GetByYearAsync(year, ct) ?? throw new KeyNotFoundException($"SchoolYear {year} not found.");
        var current = await _assignmentRepository.GetAsync(groupId, subjectId, schoolYear.Id, ct)
                      ?? throw new KeyNotFoundException("Assignment not found.");

        await _assignmentRepository.DeleteAsync(current.Id, ct);
    }

    public async Task<List<Assignment>> ListByGroupAsync(Guid groupId, int year, CancellationToken ct = default)
    {
        var schoolYear = await _schoolYearRepository.GetByYearAsync(year, ct) ?? throw new KeyNotFoundException($"SchoolYear {year} not found.");
        return await _assignmentRepository.ListByGroupAsync(groupId, schoolYear.Id, ct);
    }

    public async Task<List<Assignment>> ListByTeacherAsync(Guid teacherId, int year, CancellationToken ct = default)
    {
        var schoolYear = await _schoolYearRepository.GetByYearAsync(year, ct) ?? throw new KeyNotFoundException($"SchoolYear {year} not found.");
        return await _assignmentRepository.ListByTeacherAsync(teacherId, schoolYear.Id, ct);
    }
}
