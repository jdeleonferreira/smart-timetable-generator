using STG.Domain.Entities.Base;


namespace STG.Domain.Entities;

/// <summary>
/// Un grupo/curso (e.g., "6A") con tamaño de estudiantes.
/// </summary>
public class Group : Entity
{
    public string Grade { get; private set; } = default!;   // "6", "7", "10", etc.
    public string Label { get; private set; } = default!;   // "A", "B", etc. (opcional)
    public int Size { get; private set; }

    private Group() { }

    public Group(string grade, string label, int size)
    {
        if (string.IsNullOrWhiteSpace(grade)) throw new ArgumentException("Grade is required.");
        if (string.IsNullOrWhiteSpace(label)) label = "A";
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        Grade = grade.Trim();
        Label = label.Trim();
        Size = size;
    }

    public string Code => $"{Grade}{Label}";
}
