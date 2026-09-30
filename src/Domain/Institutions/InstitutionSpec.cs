namespace SmartTimetableGenerator.Domain.Institutions;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class InstitutionSpec : SingleResultSpecification<Institution>
{
    public static InstitutionSpec ById(InstitutionId id)
    {
        var spec = new InstitutionSpec();
        spec.Query.Where(x => x.Id == id);
        return spec;
    }
}
