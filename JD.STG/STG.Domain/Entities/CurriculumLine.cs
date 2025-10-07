using STG.Domain.Entities.Base;

namespace STG.Domain.Entities;

/// <summary>
/// Línea de malla curricular: IH (WeeklyBlocks) por grado para una asignatura.
/// </summary>
public class CurriculumLine : Entity
{
    public int Year { get; private set; }
    public string Grade { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public int WeeklyBlocks { get; private set; }   // IH

    private CurriculumLine() { }

    public CurriculumLine(int year, string grade, string subject, int weeklyBlocks)
    {
        if (year < 2000) throw new ArgumentOutOfRangeException(nameof(year));
        if (string.IsNullOrWhiteSpace(grade)) throw new ArgumentException("Grade is required.");
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required.");
        if (weeklyBlocks <= 0) throw new ArgumentOutOfRangeException(nameof(weeklyBlocks));

        Year = year;
        Grade = grade.Trim();
        Subject = subject.Trim();
        WeeklyBlocks = weeklyBlocks;
    }
}