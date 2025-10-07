using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

public class Subject : Entity
{
    public string Name { get; private set; } = default!;
    public bool NeedsLab { get; private set; }  // p.ej. Ciencias con laboratorio
    public bool MustBeDouble { get; private set; } // si requiere bloques consecutivos

    private Subject() { }

    public Subject(string name, bool needsLab = false, bool mustBeDouble = false)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Subject name is required.");
        Name = name.Trim();
        NeedsLab = needsLab;
        MustBeDouble = mustBeDouble;
    }
}
