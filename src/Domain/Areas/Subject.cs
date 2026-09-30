namespace SmartTimetableGenerator.Domain.Areas;

[ValueObject<Guid>]
public readonly partial struct SubjectId;

/// <summary>
/// Asignatura (materia) dentro de un área. La intensidad horaria por grado se define en el plan de estudios.
/// </summary>
public class Subject : Entity<SubjectId>
{
    public const int NameMaxLength = 150;
    public const int CodeMaxLength = 20;

    public string Name { get; private set; } = null!;

    /// <summary>Abreviatura para las celdas del horario (ej.: "MAT", "ING").</summary>
    public string? Code { get; private set; }

    public int Order { get; private set; }

    public bool IsActive { get; private set; }

    private Subject() { } // Needed for EF Core

    internal static Subject Create(string name, string? code, int order)
    {
        var subject = new Subject { Id = SubjectId.From(Guid.CreateVersion7()), IsActive = true };
        subject.Update(name, code, order);
        return subject;
    }

    internal void Update(string name, string? code, int order)
    {
        ThrowIfNegative(order, nameof(order));
        Name = TextRules.Required(name, NameMaxLength, nameof(name));
        Code = TextRules.Optional(code, CodeMaxLength, nameof(code))?.ToUpperInvariant();
        Order = order;
    }

    internal void SetActive(bool isActive) => IsActive = isActive;
}
