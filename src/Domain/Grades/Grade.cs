namespace SmartTimetableGenerator.Domain.Grades;

[ValueObject<Guid>]
public readonly partial struct GradeId;

/// <summary>
/// Niveles del sistema educativo colombiano.
/// </summary>
public enum EducationLevel
{
    Preschool = 0,
    Primary = 1,
    LowerSecondary = 2,
    UpperSecondary = 3
}

/// <summary>
/// Grado escolar (Preescolar, 1º … 11º). Catálogo institucional; los cursos (6ºA, 6ºB) se crean por año.
/// </summary>
public class Grade : AggregateRoot<GradeId>
{
    public const int NameMaxLength = 50;
    public const int ShortNameMaxLength = 10;

    public string Name { get; private set; } = null!;

    /// <summary>Nombre corto para tablas y encabezados (ej.: "6º", "PRE").</summary>
    public string ShortName { get; private set; } = null!;

    public EducationLevel Level { get; private set; }

    /// <summary>Orden de presentación (Preescolar = 0, 1º = 1, …).</summary>
    public int Order { get; private set; }

    private Grade() { } // Needed for EF Core

    public static Grade Create(string name, string shortName, EducationLevel level, int order)
    {
        var grade = new Grade { Id = GradeId.From(Guid.CreateVersion7()) };
        grade.Update(name, shortName, level, order);
        return grade;
    }

    public void Update(string name, string shortName, EducationLevel level, int order)
    {
        ThrowIfNegative(order, nameof(order));

        Name = TextRules.Required(name, NameMaxLength, nameof(name));
        ShortName = TextRules.Required(shortName, ShortNameMaxLength, nameof(shortName));
        Level = level;
        Order = order;
    }
}
