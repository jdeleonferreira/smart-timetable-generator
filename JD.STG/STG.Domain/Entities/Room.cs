using STG.Domain.Entities.Base;
using STG.Domain.ValueObjects;


namespace STG.Domain.Entities;

public class Room : Entity
{
    public string Name { get; private set; } = default!;
    public int Capacity { get; private set; }
    public Tags Tags { get; } = new();

    private Room() { }

    public Room(string name, int capacity, IEnumerable<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Room name is required.");
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Name = name.Trim();
        Capacity = capacity;
        if (tags != null) foreach (var t in tags) Tags.Add(t);
    }

    public bool HasTag(string tag) => Tags.Contains(tag);
}
