namespace SmartTimetableGenerator.Domain.Campuses;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class CampusSpec : SingleResultSpecification<Campus>
{
    public static CampusSpec ById(CampusId id)
    {
        var spec = new CampusSpec();
        spec.Query.Where(x => x.Id == id)
            .Include(x => x.Shifts)
            .ThenInclude(s => s.BellSchedules);
        return spec;
    }
}
