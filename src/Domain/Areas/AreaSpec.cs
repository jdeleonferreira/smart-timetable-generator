namespace SmartTimetableGenerator.Domain.Areas;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class AreaSpec : SingleResultSpecification<Area>
{
    public static AreaSpec ById(AreaId id)
    {
        var spec = new AreaSpec();
        spec.Query.Where(x => x.Id == id)
            .Include(x => x.Subjects);
        return spec;
    }

    public static AreaSpec BySubject(SubjectId subjectId)
    {
        var spec = new AreaSpec();
        spec.Query
            .Where(x => x.Subjects.Any(s => s.Id == subjectId))
            .Include(x => x.Subjects);
        return spec;
    }
}
