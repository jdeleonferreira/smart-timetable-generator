namespace SmartTimetableGenerator.Domain.Common.Calendar;

/// <summary>
/// Calcula los días festivos de Colombia (Ley 51 de 1983 "Ley Emiliani" y festivos religiosos móviles).
/// </summary>
public static class ColombianHolidays
{
    public static IReadOnlyList<(DateOnly Date, string Name)> ForYear(int year)
    {
        ThrowIfLessThan(year, 1984, nameof(year));
        ThrowIfGreaterThan(year, 2200, nameof(year));

        var easter = EasterSunday(year);

        List<(DateOnly Date, string Name)> holidays =
        [
            // Fijos
            (new DateOnly(year, 1, 1), "Año Nuevo"),
            (new DateOnly(year, 5, 1), "Día del Trabajo"),
            (new DateOnly(year, 7, 20), "Día de la Independencia"),
            (new DateOnly(year, 8, 7), "Batalla de Boyacá"),
            (new DateOnly(year, 12, 8), "Inmaculada Concepción"),
            (new DateOnly(year, 12, 25), "Navidad"),

            // Trasladables al lunes siguiente (Ley Emiliani)
            (NextMonday(new DateOnly(year, 1, 6)), "Día de los Reyes Magos"),
            (NextMonday(new DateOnly(year, 3, 19)), "Día de San José"),
            (NextMonday(new DateOnly(year, 6, 29)), "San Pedro y San Pablo"),
            (NextMonday(new DateOnly(year, 8, 15)), "Asunción de la Virgen"),
            (NextMonday(new DateOnly(year, 10, 12)), "Día de la Raza"),
            (NextMonday(new DateOnly(year, 11, 1)), "Todos los Santos"),
            (NextMonday(new DateOnly(year, 11, 11)), "Independencia de Cartagena"),

            // Dependientes de la Pascua
            (easter.AddDays(-3), "Jueves Santo"),
            (easter.AddDays(-2), "Viernes Santo"),
            (NextMonday(easter.AddDays(39)), "Ascensión del Señor"),
            (NextMonday(easter.AddDays(60)), "Corpus Christi"),
            (NextMonday(easter.AddDays(68)), "Sagrado Corazón")
        ];

        return holidays.OrderBy(h => h.Date).ToList();
    }

    public static bool IsHoliday(DateOnly date) => ForYear(date.Year).Any(h => h.Date == date);

    /// <summary>
    /// Domingo de Pascua (algoritmo anónimo gregoriano / Meeus-Jones-Butcher).
    /// </summary>
    public static DateOnly EasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = ((19 * a) + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        var m = (a + (11 * h) + (22 * l)) / 451;
        var month = (h + l - (7 * m) + 114) / 31;
        var day = ((h + l - (7 * m) + 114) % 31) + 1;

        return new DateOnly(year, month, day);
    }

    private static DateOnly NextMonday(DateOnly date)
    {
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(daysUntilMonday);
    }
}
