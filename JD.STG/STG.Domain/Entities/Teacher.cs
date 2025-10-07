using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

public class Teacher : Entity
{
    public string Name { get; private set; } = default!;
    /// <summary>Materias que puede dictar (por nombre de Subject).</summary>
    public HashSet<string> Subjects { get; } = new(StringComparer.OrdinalIgnoreCase);

    private Teacher() { }

    public Teacher(string name, IEnumerable<string>? subjects = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Teacher name is required.");
        Name = name.Trim();
        if (subjects != null)
            foreach (var s in subjects) AddSubject(s);
    }

    public bool AddSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) return false;
        return Subjects.Add(subject.Trim());
    }
}
