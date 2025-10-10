using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Represents a teacher within the academic domain.
/// Each teacher may be qualified to teach one or more subjects.
/// </summary>
public sealed class Teacher : Entity
{
    /// <summary>
    /// Teacher’s full name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Collection of subject names (normalized) that the teacher can teach.
    /// In a future iteration this may reference <see cref="Subject"/> entities by ID.
    /// </summary>
    private readonly HashSet<string> _subjects = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Exposes the subjects as a read-only collection.
    /// </summary>
    public IReadOnlyCollection<string> Subjects => _subjects;

    private Teacher() { } // EF Core constructor

    /// <summary>
    /// Creates a new teacher profile with an optional list of teachable subjects.
    /// </summary>
    /// <param name="name">Teacher's full name.</param>
    /// <param name="subjects">Optional list of subject names the teacher can teach.</param>
    public Teacher(string name, IEnumerable<string>? subjects = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Teacher name is required.", nameof(name));

        SetCreated();
        Id = Guid.NewGuid();

        Name = name.Trim();

        if (subjects is not null)
        {
            foreach (var subject in subjects)
                AddSubject(subject);
        }
    }

    /// <summary>
    /// Adds a subject qualification for this teacher.
    /// Returns true if the subject was successfully added.
    /// </summary>
    public bool AddSubject(string subject, string? modifiedBy = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return false;

        var added = _subjects.Add(subject.Trim());
        if (added)
            SetModified(modifiedBy);

        return added;
    }

    /// <summary>
    /// Removes a subject qualification, if it exists.
    /// Returns true if the subject was removed.
    /// </summary>
    public bool RemoveSubject(string subject, string? modifiedBy = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return false;

        var removed = _subjects.Remove(subject.Trim());
        if (removed)
            SetModified(modifiedBy);

        return removed;
    }

    /// <summary>
    /// Changes the teacher's display name.
    /// </summary>
    public void Rename(string newName, string? modifiedBy = null)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name is required.", nameof(newName));

        Name = newName.Trim();
        SetModified(modifiedBy);
    }

    public override string ToString() =>
        $"{Name} ({_subjects.Count} subjects)";
}
