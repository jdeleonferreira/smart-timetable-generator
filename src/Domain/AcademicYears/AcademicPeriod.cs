namespace SmartTimetableGenerator.Domain.AcademicYears;

[ValueObject<Guid>]
public readonly partial struct AcademicPeriodId;

/// <summary>
/// Periodo académico (ej.: Periodo 1). Cantidad y fechas configurables por año.
/// </summary>
public class AcademicPeriod : Entity<AcademicPeriodId>
{
    public const int NameMaxLength = 50;

    public int Number { get; private set; }
    public string Name { get; private set; } = null!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    private AcademicPeriod() { } // Needed for EF Core

    internal static AcademicPeriod Create(string name, DateOnly startDate, DateOnly endDate)
    {
        var period = new AcademicPeriod { Id = AcademicPeriodId.From(Guid.CreateVersion7()) };
        period.Update(name, startDate, endDate);
        return period;
    }

    internal void Update(string name, DateOnly startDate, DateOnly endDate)
    {
        Name = TextRules.Required(name, NameMaxLength, nameof(name));
        StartDate = startDate;
        EndDate = endDate;
    }

    internal void SetNumber(int number) => Number = number;
}
