using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Child entity of <see cref="StudyPlan"/> representing weekly hours for a (Grade, Subject).
/// </summary>
/// <remarks>
/// Invariants:
/// - WeeklyHours in [0..60].
/// - Identified within a plan by the pair (GradeId, SubjectId).
/// </remarks>
public sealed class StudyPlanEntry : Entity
{
    private StudyPlanEntry() { } // EF

    public StudyPlanEntry(Guid id, Guid studyPlanId, Guid gradeId, Guid subjectId, byte weeklyHours, string? notes = null)
    {
        Id = id == default ? Guid.NewGuid() : id;
        if (studyPlanId == Guid.Empty) throw new ArgumentException("StudyPlanId cannot be empty.", nameof(studyPlanId));
        if (gradeId == Guid.Empty) throw new ArgumentException("GradeId cannot be empty.", nameof(gradeId));
        if (subjectId == Guid.Empty) throw new ArgumentException("SubjectId cannot be empty.", nameof(subjectId));
        SetWeeklyHours(weeklyHours);

        StudyPlanId = studyPlanId;
        GradeId = gradeId;
        SubjectId = subjectId;
        SetNotes(notes);
    }


    /// <summary>FK to owning <see cref="StudyPlan"/>.</summary>
    public Guid StudyPlanId { get; private set; }

    /// <summary>FK to <see cref="Grade"/> catalog.</summary>
    public Guid GradeId { get; private set; }

    /// <summary>FK to <see cref="Subject"/> catalog.</summary>
    public Guid SubjectId { get; private set; }

    /// <summary>Total weekly hours allocated to this subject for the grade.</summary>
    public byte WeeklyHours { get; private set; }

    /// <summary>Optional free-text notes.</summary>
    public string? Notes { get; private set; }

    /// <summary>Changes the hours while enforcing range invariant.</summary>
    public void SetWeeklyHours(byte weeklyHours)
    {
        if (weeklyHours > 60)
            throw new ArgumentOutOfRangeException(nameof(weeklyHours), "WeeklyHours must be between 0 and 60.");
        WeeklyHours = weeklyHours;
    }

    /// <summary>Updates notes, normalizing empty strings to null.</summary>
    public void SetNotes(string? notes)
        => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes!.Trim();
}
