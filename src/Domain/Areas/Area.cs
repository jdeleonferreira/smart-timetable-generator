namespace SmartTimetableGenerator.Domain.Areas;

[ValueObject<Guid>]
public readonly partial struct AreaId;

/// <summary>
/// Área de conocimiento (ej.: Matemáticas, Humanidades). Agrupa asignaturas.
/// A los docentes se les asignan áreas para saber qué asignaturas pueden dictar.
/// </summary>
public class Area : AggregateRoot<AreaId>
{
    public const int NameMaxLength = 150;

    private readonly List<Subject> _subjects = [];

    public string Name { get; private set; } = null!;

    public int Order { get; private set; }

    public IReadOnlyList<Subject> Subjects => _subjects.AsReadOnly();

    private Area() { } // Needed for EF Core

    public static Area Create(string name, int order)
    {
        var area = new Area { Id = AreaId.From(Guid.CreateVersion7()) };
        area.Update(name, order);
        return area;
    }

    public void Update(string name, int order)
    {
        ThrowIfNegative(order, nameof(order));
        Name = TextRules.Required(name, NameMaxLength, nameof(name));
        Order = order;
    }

    public Subject? FindSubject(SubjectId subjectId) => _subjects.FirstOrDefault(s => s.Id == subjectId);

    public ErrorOr<Subject> AddSubject(string name, string? code = null, int? order = null)
    {
        if (_subjects.Any(s => string.Equals(s.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return AreaErrors.DuplicateSubjectName;

        var subject = Subject.Create(name, code, order ?? _subjects.Count);
        _subjects.Add(subject);
        return subject;
    }

    public ErrorOr<Success> UpdateSubject(SubjectId subjectId, string name, string? code, int order)
    {
        var subject = FindSubject(subjectId);
        if (subject is null)
            return AreaErrors.SubjectNotFound;

        if (_subjects.Any(s => s.Id != subjectId && string.Equals(s.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return AreaErrors.DuplicateSubjectName;

        subject.Update(name, code, order);
        return Result.Success;
    }

    /// <summary>
    /// Las asignaturas no se eliminan (pueden estar en planes de años anteriores); se desactivan.
    /// </summary>
    public ErrorOr<Success> SetSubjectActive(SubjectId subjectId, bool isActive)
    {
        var subject = FindSubject(subjectId);
        if (subject is null)
            return AreaErrors.SubjectNotFound;

        subject.SetActive(isActive);
        return Result.Success;
    }
}
