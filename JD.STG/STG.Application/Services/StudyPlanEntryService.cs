using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;

/// <summary>
/// Application service for managing individual StudyPlan entries (no CQRS/MediatR).
/// </summary>
public sealed class StudyPlanEntryService
{
    private readonly IStudyPlanRepository _studyPlanRepository;
    private readonly IStudyPlanEntryRepository _studyPlanEntryRepository;
    private readonly IGradeRepository _gradeRepository;
    private readonly ISubjectRepository _subjectRepository;

    public StudyPlanEntryService(
        IStudyPlanRepository plans,
        IStudyPlanEntryRepository entries,
        IGradeRepository grades,
        ISubjectRepository subjects)
    {
        _studyPlanRepository = plans;
        _studyPlanEntryRepository = entries;
        _gradeRepository = grades;
        _subjectRepository = subjects;
    }

    /// <summary>Create a new entry for (plan, grade, subject) with weekly hours.</summary>
    public async Task<Guid> CreateAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, byte weeklyHours, string? notes = null, CancellationToken ct = default)
    {
        _ = await _studyPlanRepository.GetByIdAsync(studyPlanId, ct) ?? throw new KeyNotFoundException("StudyPlan not found.");
        _ = await _gradeRepository.GetByIdAsync(gradeId, ct) ?? throw new KeyNotFoundException("Grade not found.");
        _ = await _subjectRepository.GetByIdAsync(subjectId, ct) ?? throw new KeyNotFoundException("Subject not found.");

        var dup = await _studyPlanEntryRepository.FindAsync(studyPlanId, gradeId, subjectId, ct);
        if (dup is not null)
            throw new InvalidOperationException("An entry for this (Grade, Subject) already exists in the plan.");

        var entity = new StudyPlanEntry(Guid.NewGuid(), studyPlanId, gradeId, subjectId, weeklyHours, notes);
        return await _studyPlanEntryRepository.AddAsync(entity, ct);
    }

    /// <summary>Upserts (create or update) an entry for (plan, grade, subject).</summary>
    public async Task UpsertAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, byte weeklyHours, string? notes = null, CancellationToken ct = default)
    {
        var existing = await _studyPlanEntryRepository.FindAsync(studyPlanId, gradeId, subjectId, ct);
        if (existing is null)
        {
            await CreateAsync(studyPlanId, gradeId, subjectId, weeklyHours, notes, ct);
            return;
        }
        existing.SetWeeklyHours(weeklyHours);
        existing.SetNotes(notes);
        await _studyPlanEntryRepository.UpdateAsync(existing, ct);
    }

    /// <summary>Update hours for an existing entry.</summary>
    public async Task UpdateHoursAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, byte weeklyHours, CancellationToken ct = default)
    {
        var existing = await _studyPlanEntryRepository.FindAsync(studyPlanId, gradeId, subjectId, ct)
                       ?? throw new KeyNotFoundException("StudyPlanEntry not found.");
        existing.SetWeeklyHours(weeklyHours);
        await _studyPlanEntryRepository.UpdateAsync(existing, ct);
    }

    /// <summary>Remove the entry (plan, grade, subject) if present.</summary>
    public async Task RemoveAsync(Guid studyPlanId, Guid gradeId, Guid subjectId, CancellationToken ct = default)
        => await _studyPlanEntryRepository.DeleteByKeyAsync(studyPlanId, gradeId, subjectId, ct);

    public async Task<List<StudyPlanEntry>> ListByPlanAsync(Guid studyPlanId, CancellationToken ct = default)
        => await _studyPlanEntryRepository.ListByPlanAsync(studyPlanId, ct);

    public async Task<List<StudyPlanEntry>> ListByPlanAndGradeAsync(Guid studyPlanId, Guid gradeId, CancellationToken ct = default)
        => await _studyPlanEntryRepository.ListByPlanAndGradeAsync(studyPlanId, gradeId, ct);
}
