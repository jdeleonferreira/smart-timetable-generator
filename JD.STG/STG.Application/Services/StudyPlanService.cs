// src/STG.Application/Curriculum/StudyPlanService.cs
using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;

/// <summary>
/// Application service that manages StudyPlans and their entries (no CQRS/MediatR).
/// </summary>
public sealed class StudyPlanService
{
    private readonly IStudyPlanRepository _studyPlanRepository;
    private readonly ISchoolYearRepository _schoolYearRepository;
    private readonly IGradeRepository _gradeRepository;
    private readonly ISubjectRepository _subjectRepository;

    public StudyPlanService(
        IStudyPlanRepository plans,
        ISchoolYearRepository years,
        IGradeRepository grades,
        ISubjectRepository subjects)
    {
        _studyPlanRepository = plans;
        _schoolYearRepository = years;
        _gradeRepository = grades;
        _subjectRepository = subjects;
    }

    /// <summary>
    /// Creates a StudyPlan for a given year (idempotent on SchoolYear: one plan per year).
    /// </summary>
    public async Task<Guid> CreateForYearAsync(int year, string? name = null, string? notes = null, CancellationToken ct = default)
    {
        var schoolYear = await _schoolYearRepository.GetByYearAsync(year, ct)
                         ?? throw new KeyNotFoundException($"SchoolYear {year} not found.");

        // prevent duplicates via unique index at DB; check first for clarity
        var existing = await _studyPlanRepository.GetByYearAsync(year, ct);
        if (existing is not null) return existing.Id;

        var plan = new StudyPlan(Guid.NewGuid(), schoolYear.Id, name ?? $"Plan {year}", notes);
        return await _studyPlanRepository.AddAsync(plan, ct);
    }

    /// <summary>Upserts a single entry (Grade, Subject, WeeklyHours) in the plan for the given year.</summary>
    public async Task UpsertEntryAsync(int year, Guid gradeId, Guid subjectId, byte weeklyHours, string? notes = null, CancellationToken ct = default)
    {
        // Validate catalogs existence (defensive checks)
        _ = await _gradeRepository.GetByIdAsync(gradeId, ct) ?? throw new KeyNotFoundException("Grade not found.");
        _ = await _subjectRepository.GetByIdAsync(subjectId, ct) ?? throw new KeyNotFoundException("Subject not found.");

        var plan = await _studyPlanRepository.GetByYearAsync(year, ct) ?? throw new KeyNotFoundException($"StudyPlan for year {year} not found.");
        // Use domain method for invariants if you loaded the aggregate tracked; for simplicity call repo helper
        await _studyPlanRepository.UpsertEntryAsync(plan.Id, gradeId, subjectId, weeklyHours, notes, ct);
    }

    /// <summary>Bulk upsert entries for the target year.</summary>
    public async Task UpsertEntriesAsync(int year, IEnumerable<(Guid gradeId, Guid subjectId, byte weeklyHours, string? notes)> rows, CancellationToken ct = default)
    {
        var plan = await _studyPlanRepository.GetByYearAsync(year, ct) ?? throw new KeyNotFoundException($"StudyPlan for year {year} not found.");

        foreach (var row in rows)
        {
            _ = await _gradeRepository.GetByIdAsync(row.gradeId, ct) ?? throw new KeyNotFoundException("Grade not found.");
            _ = await _subjectRepository.GetByIdAsync(row.subjectId, ct) ?? throw new KeyNotFoundException("Subject not found.");

            await _studyPlanRepository.UpsertEntryAsync(plan.Id, row.gradeId, row.subjectId, row.weeklyHours, row.notes, ct);
        }
    }

    /// <summary>Removes the entry identified by (Grade, Subject) for the given year.</summary>
    public async Task RemoveEntryAsync(int year, Guid gradeId, Guid subjectId, CancellationToken ct = default)
    {
        var plan = await _studyPlanRepository.GetByYearAsync(year, ct) ?? throw new KeyNotFoundException($"StudyPlan for year {year} not found.");
        await _studyPlanRepository.RemoveEntryAsync(plan.Id, gradeId, subjectId, ct);
    }

    /// <summary>Returns the plan (with entries) for the given year.</summary>
    public Task<StudyPlan?> GetByYearAsync(int year, CancellationToken ct = default) => _studyPlanRepository.GetByYearAsync(year, ct);
}
