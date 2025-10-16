namespace STG.Domain.ValueObjects;

/// <summary>
/// Represents a collection of case-insensitive tags.
/// A tag is a lightweight string identifier used for categorization or filtering.
/// </summary>
public sealed class Tags : IEquatable<Tags>
{
    private readonly HashSet<string> _values;

    /// <summary>
    /// Gets the read-only list of tags. Stored case-insensitively and trimmed.
    /// </summary>
    public IReadOnlyCollection<string> Values => _values;

    /// <summary>
    /// Initializes an empty tag set.
    /// </summary>
    public Tags()
    {
        _values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes the tag set with a predefined collection of tags.
    /// </summary>
    /// <param name="tags">Collection of tag strings; null values are ignored.</param>
    public Tags(IEnumerable<string>? tags)
        : this()
    {
        foreach (var tag in tags ?? Array.Empty<string>())
            Add(tag);
    }

    /// <summary>
    /// Adds a tag to the set. Returns <c>true</c> if it was added successfully.
    /// Tags are trimmed and compared case-insensitively.
    /// </summary>
    /// <param name="tag">Tag to add.</param>
    public bool Add(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return false;

        return _values.Add(tag.Trim());
    }

    /// <summary>
    /// Returns <c>true</c> if the specified tag is already present in the set.
    /// </summary>
    /// <param name="tag">Tag to check.</param>
    public bool Contains(string tag) =>
        !string.IsNullOrWhiteSpace(tag) && _values.Contains(tag.Trim());

    /// <summary>
    /// Provides value-based equality comparison between two <see cref="Tags"/> instances.
    /// </summary>
    public bool Equals(Tags? other)
    {
        if (other is null) return false;
        return _values.SetEquals(other._values);
    }

    public override bool Equals(object? obj) => Equals(obj as Tags);

    public override int GetHashCode()
    {
        // Combine hashes of all tags for stable value equality
        int hash = 17;
        foreach (var tag in _values.OrderBy(v => v))
            hash = HashCode.Combine(hash, StringComparer.OrdinalIgnoreCase.GetHashCode(tag));
        return hash;
    }

    public static bool operator ==(Tags? a, Tags? b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(Tags? a, Tags? b) => !(a == b);

    /// <summary>
    /// Returns a comma-separated representation of all tags.
    /// </summary>
    public override string ToString() => string.Join(", ", _values);
}
