using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Aggregate root that defines the curricular plan for a given school year.
/// Holds the weekly hours per (Grade, Subject) through <see cref="StudyPlanEntry"/>.
/// </summary>
/// <remarks>
/// Invariants:
/// - One StudyPlan per SchoolYear (enforced at persistence).
/// - Entries are unique per (GradeId, SubjectId) within the same plan.
/// - WeeklyHours in [0..40].
/// </remarks>
public sealed class StudyPlan : Entity
{
    private readonly List<StudyPlanEntry> _entries = new();
    private StudyPlan() { } // EF

    public StudyPlan(Guid id, Guid schoolYearId, string name, string? notes = null)
    {
        Id = id == default ? Guid.NewGuid() : id;
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId cannot be empty.", nameof(schoolYearId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));

        SchoolYearId = schoolYearId;
        Name = name.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes!.Trim();
    }

    /// <summary>FK to the academic year this plan belongs to.</summary>
    public Guid SchoolYearId { get; private set; }

    /// <summary>Human-friendly name (e.g., "Plan 2025").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Optional notes.</summary>
    public string? Notes { get; private set; }

    /// <summary>Entries defining weekly hours per grade and subject.</summary>
    public IReadOnlyCollection<StudyPlanEntry> Entries => _entries;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
        Name = name.Trim();
    }

    public void SetNotes(string? notes) => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes!.Trim();

    /// <summary>Creates or updates an entry for (grade, subject) with the given weekly hours.</summary>
    public void UpsertEntry(Guid gradeId, Guid subjectId, byte weeklyHours, string? notes = null)
    {
        if (gradeId == Guid.Empty) throw new ArgumentException("GradeId cannot be empty.", nameof(gradeId));
        if (subjectId == Guid.Empty) throw new ArgumentException("SubjectId cannot be empty.", nameof(subjectId));
        if (weeklyHours > 40) throw new ArgumentOutOfRangeException(nameof(weeklyHours), "WeeklyHours must be between 0 and 40.");

        var existing = _entries.FirstOrDefault(e => e.GradeId == gradeId && e.SubjectId == subjectId);
        if (existing is null)
        {
            _entries.Add(new StudyPlanEntry(Guid.NewGuid(), Id, gradeId, subjectId, weeklyHours, notes));
        }
        else
        {
            existing.SetWeeklyHours(weeklyHours);
            existing.SetNotes(notes);
        }
    }

    /// <summary>Removes the entry identified by (grade, subject) if present.</summary>
    public void RemoveEntry(Guid gradeId, Guid subjectId)
    {
        var existing = _entries.FirstOrDefault(e => e.GradeId == gradeId && e.SubjectId == subjectId);
        if (existing is not null) _entries.Remove(existing);
    }
}