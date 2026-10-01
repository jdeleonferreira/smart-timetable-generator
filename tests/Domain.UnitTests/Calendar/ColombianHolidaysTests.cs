using SmartTimetableGenerator.Domain.Common.Calendar;

namespace SmartTimetableGenerator.Domain.UnitTests.Calendar;

public class ColombianHolidaysTests
{
    [Fact]
    public void ForYear_2026_ShouldMatchOfficialCalendar()
    {
        DateOnly[] expected =
        [
            new(2026, 1, 1), new(2026, 1, 12), new(2026, 3, 23), new(2026, 4, 2), new(2026, 4, 3),
            new(2026, 5, 1), new(2026, 5, 18), new(2026, 6, 8), new(2026, 6, 15), new(2026, 6, 29),
            new(2026, 7, 20), new(2026, 8, 7), new(2026, 8, 17), new(2026, 10, 12), new(2026, 11, 2),
            new(2026, 11, 16), new(2026, 12, 8), new(2026, 12, 25)
        ];

        var holidays = ColombianHolidays.ForYear(2026).Select(h => h.Date);

        holidays.Should().Equal(expected);
    }

    [Theory]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    public void EasterSunday_ShouldBeCorrect(int year, int month, int day)
    {
        ColombianHolidays.EasterSunday(year).Should().Be(new DateOnly(year, month, day));
    }

    [Fact]
    public void MovableHolidays_ShouldFallOnMonday()
    {
        var movable = new[] { "Reyes", "San José", "San Pedro", "Asunción", "Raza", "Todos los Santos", "Cartagena", "Ascensión", "Corpus", "Sagrado" };

        var holidays = ColombianHolidays.ForYear(2027)
            .Where(h => movable.Any(m => h.Name.Contains(m, StringComparison.Ordinal)));

        holidays.Should().OnlyContain(h => h.Date.DayOfWeek == DayOfWeek.Monday);
    }
}
