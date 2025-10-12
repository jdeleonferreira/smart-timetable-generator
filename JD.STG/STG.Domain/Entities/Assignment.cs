// src/STG.Domain/Entities/Assignment.cs
using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Operational instantiation of teaching load for a Group in a SchoolYear:
/// (Group, Subject, SchoolYear) with weekly hours and optional Teacher.
/// </summary>
/// <remarks>
/// Invariants:
/// - WeeklyHours in [0..60].
/// - Unique per (GroupId, SubjectId, SchoolYearId) at persistence.
/// - TeacherId is optional in MVP.
/// </remarks>
public sealed class Assignment : Entity
{
    /// <summary>FK to <see cref="Group"/> catalog.</summary>
    public Guid GroupId { get; private set; }

    /// <summary>FK to <see cref="Subject"/> catalog.</summary>
    public Guid SubjectId { get; private set; }

    /// <summary>FK to <see cref="SchoolYear"/> aggregate.</summary>
    public Guid SchoolYearId { get; private set; }

    /// <summary>Optional FK to Teacher (catalog/personnel).</summary>
    public Guid? TeacherId { get; private set; }

    /// <summary>Total weekly hours scheduled for this (Group, Subject, Year).</summary>
    public byte WeeklyHours { get; private set; }

    /// <summary>Optional free-text notes.</summary>
    public string? Notes { get; private set; }

    // Navigations (optional for queries; keep them if you use Includes)
    public Group Group { get; private set; } = null!;
    public Subject Subject { get; private set; } = null!;
    // public Teacher Teacher { get; private set; } = null!; // add if/when Teacher entity exists
    public SchoolYear SchoolYear { get; private set; } = null!;

    private Assignment() { } // EF

    public Assignment(Guid id, Guid groupId, Guid subjectId, Guid schoolYearId, byte weeklyHours, Guid? teacherId = null, string? notes = null)
    {
        Id = id == default ? Guid.NewGuid() : id;
        SetGroup(groupId);
        SetSubject(subjectId);
        SetSchoolYear(schoolYearId);
        SetWeeklyHours(weeklyHours);
        SetTeacher(teacherId);
        SetNotes(notes);
    }

    // Guards / behavior
    public void SetGroup(Guid groupId)
    {
        if (groupId == Guid.Empty) throw new ArgumentException("GroupId cannot be empty.", nameof(groupId));
        GroupId = groupId;
    }
    public void SetSubject(Guid subjectId)
    {
        if (subjectId == Guid.Empty) throw new ArgumentException("SubjectId cannot be empty.", nameof(subjectId));
        SubjectId = subjectId;
    }
    public void SetSchoolYear(Guid schoolYearId)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId cannot be empty.", nameof(schoolYearId));
        SchoolYearId = schoolYearId;
    }
    public void SetWeeklyHours(byte weeklyHours)
    {
        if (weeklyHours > 60) throw new ArgumentOutOfRangeException(nameof(weeklyHours), "WeeklyHours must be between 0 and 60.");
        WeeklyHours = weeklyHours;
    }
    public void SetTeacher(Guid? teacherId)
    {
        if (teacherId.HasValue && teacherId.Value == Guid.Empty)
            throw new ArgumentException("TeacherId cannot be empty when provided.", nameof(teacherId));
        TeacherId = teacherId;
    }
    public void SetNotes(string? notes) => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes!.Trim();
}
