// FILE: STG.Domain/Entities/Timetable.cs
using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Aggregate representing the scheduled timetable for a Group in a SchoolYear.
/// Domain rules:
/// 1) Belongs to (SchoolYearId, GroupId) and owns multiple entries.
/// 2) Must not contain duplicate (DayOfWeek, PeriodNumber) cells.
/// 3) Overlap checks for teachers are typically enforced at service/policy level,
///    but helper detection methods are provided.
/// </summary>
public sealed class Timetable : Entity
{
    public Guid SchoolYearId { get; private set; }
    public Guid GroupId { get; private set; }

    public double? Score { get; private set; }

    public IReadOnlyCollection<TimetableEntry> Entries => _entries.AsReadOnly();
    private readonly List<TimetableEntry> _entries = new();

    private Timetable() { } // EF

    public Timetable(Guid schoolYearId, Guid groupId)
    {
        if (schoolYearId == Guid.Empty) throw new ArgumentException("SchoolYearId is required.", nameof(schoolYearId));
        if (groupId == Guid.Empty) throw new ArgumentException("GroupId is required.", nameof(groupId));

        Id = Guid.NewGuid();
        SchoolYearId = schoolYearId;
        GroupId = groupId;
        SetCreated();
    }

    /// <summary>Adds an entry ensuring no duplicate cell exists in this timetable.</summary>
    public Timetable AddEntry(TimetableEntry entry, string? modifiedBy = null)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        if (entry.TimetableId != Id) throw new InvalidOperationException("Entry must target this Timetable.");
        if (_entries.Any(e => e.DayOfWeek == entry.DayOfWeek && e.PeriodNumber == entry.PeriodNumber))
            throw new InvalidOperationException("Duplicate (DayOfWeek, PeriodNumber) in timetable.");

        _entries.Add(entry);
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Sets the computed score.</summary>
    public Timetable SetScore(double? score, string? modifiedBy = null)
    {
        Score = score;
        SetModified(modifiedBy);
        return this;
    }

    /// <summary>Detects if the teacher is already scheduled in this cell.</summary>
    public bool HasTeacherAt(DayOfWeek day, int period, Guid teacherId)
        => _entries.Any(e => e.DayOfWeek == day && e.PeriodNumber == period && e.AssignmentTeacherId == teacherId);
}
