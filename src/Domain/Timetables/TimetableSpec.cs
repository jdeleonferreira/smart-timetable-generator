namespace SmartTimetableGenerator.Domain.Timetables;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class TimetableSpec : SingleResultSpecification<Timetable>
{
    public static TimetableSpec ById(TimetableId id)
    {
        var spec = new TimetableSpec();
        spec.Query.Where(x => x.Id == id)
            .Include(x => x.Lessons);
        return spec;
    }
}
