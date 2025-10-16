using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Represents a teacher/personnel who can be assigned to class Assignments.
/// </summary>
/// <remarks>
/// Invariants:
/// - FullName: required (non-empty).
/// - Email: optional, unique when present.
/// - IsActive: soft on/off for scheduling/selection.
/// - MaxWeeklyLoad: optional guard for scheduling (MVP default null).
/// </remarks>
public sealed class Teacher : Entity
{
    /// <summary>Display name.</summary>
    public string FullName { get; private set; } = null!;

    /// <summary>Optional unique email.</summary>
    public string? Email { get; private set; }

    /// <summary>Optional weekly hours cap for scheduling.</summary>
    public byte? MaxWeeklyLoad { get; private set; }

    /// <summary>Soft on/off flag.</summary>
    public bool IsActive { get; private set; }

    private Teacher() { } // EF

    public Teacher(Guid id, string fullName, string? email = null, byte? maxWeeklyLoad = null, bool isActive = true)
    {
        Id = id == default ? Guid.NewGuid() : id;
        Rename(fullName);
        SetEmail(email);
        SetMaxWeeklyLoad(maxWeeklyLoad);
        IsActive = isActive;
    }


    public void Rename(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty.", nameof(fullName));
        FullName = fullName.Trim();
    }

    public void SetEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            Email = null;
            return;
        }
        var trimmed = email.Trim();
        if (!trimmed.Contains("@") || trimmed.StartsWith("@") || trimmed.EndsWith("@"))
            throw new ArgumentException("Invalid email format.", nameof(email));
        Email = trimmed;
    }

    public void SetMaxWeeklyLoad(byte? hours)
    {
        if (hours is > 40) throw new ArgumentOutOfRangeException(nameof(hours), "MaxWeeklyLoad must be <= 40.");
        MaxWeeklyLoad = hours;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
