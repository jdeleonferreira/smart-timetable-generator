namespace SmartTimetableGenerator.Domain.AcademicYears;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class AcademicYearSpec : SingleResultSpecification<AcademicYear>
{
    public static AcademicYearSpec ById(AcademicYearId id)
    {
        var spec = new AcademicYearSpec();
        spec.Query.Where(x => x.Id == id)
            .Include(x => x.Periods)
            .Include(x => x.CalendarEntries);
        return spec;
    }

    public static AcademicYearSpec ByYear(int year)
    {
        var spec = new AcademicYearSpec();
        spec.Query
            .Where(x => x.Year == year)
            .Include(x => x.Periods)
            .Include(x => x.CalendarEntries);
        return spec;
    }
}
