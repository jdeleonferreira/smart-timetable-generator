using SmartTimetableGenerator.Domain.AcademicYears;

namespace SmartTimetableGenerator.Domain.StudyPlans;

/// <summary>
/// IH semanal de una asignatura en un periodo académico concreto.
/// </summary>
public record PeriodHours : IValueObject
{
    // Private setters needed for EF
    public AcademicPeriodId AcademicPeriodId { get; private set; }

    public int WeeklyHours { get; private set; }

    public PeriodHours(AcademicPeriodId academicPeriodId, int weeklyHours)
    {
        ThrowIfNegative(weeklyHours, nameof(weeklyHours));
        AcademicPeriodId = academicPeriodId;
        WeeklyHours = weeklyHours;
    }
}
