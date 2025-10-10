using STG.Domain.Entities.Base;
using STG.Domain.ValueObjects;

[Obsolete("Not used in MVP. Planned for scheduling v2.")]
public sealed class Room : Entity
{
    public string Name { get; private set; } = string.Empty;
    public ushort Capacity { get; private set; }
    public Tags Tags { get; } = new();

    private Room() { }

    public Room(string name, ushort capacity, IEnumerable<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Room name is required.", nameof(name));
        if (capacity == 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

        SetCreated();
        Id = Guid.NewGuid();
        Name = name.Trim();
        Capacity = capacity;
        if (tags is not null)
            foreach (var tag in tags)
                Tags.Add(tag);
    }

    public bool HasTag(string tag) => Tags.Contains(tag);
}
