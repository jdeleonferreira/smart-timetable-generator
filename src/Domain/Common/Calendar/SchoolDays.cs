namespace SmartTimetableGenerator.Domain.Common.Calendar;

/// <summary>
/// Días de la semana en que hay clase (combinables).
/// </summary>
[Flags]
public enum SchoolDays
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
    MondayToFriday = Monday | Tuesday | Wednesday | Thursday | Friday,
    MondayToSaturday = MondayToFriday | Saturday
}

public static class SchoolDaysExtensions
{
    public static SchoolDays ToSchoolDay(this DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => SchoolDays.Monday,
        DayOfWeek.Tuesday => SchoolDays.Tuesday,
        DayOfWeek.Wednesday => SchoolDays.Wednesday,
        DayOfWeek.Thursday => SchoolDays.Thursday,
        DayOfWeek.Friday => SchoolDays.Friday,
        DayOfWeek.Saturday => SchoolDays.Saturday,
        DayOfWeek.Sunday => SchoolDays.Sunday,
        _ => SchoolDays.None
    };

    public static bool Includes(this SchoolDays days, DayOfWeek day) => (days & day.ToSchoolDay()) != SchoolDays.None;

    /// <summary>
    /// Días incluidos, en orden de lunes a domingo.
    /// </summary>
    public static IReadOnlyList<DayOfWeek> ToDaysOfWeek(this SchoolDays days)
    {
        DayOfWeek[] ordered =
        [
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        ];

        return ordered.Where(d => days.Includes(d)).ToList();
    }
}
