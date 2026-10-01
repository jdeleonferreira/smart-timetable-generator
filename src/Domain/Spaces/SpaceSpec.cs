namespace SmartTimetableGenerator.Domain.Spaces;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class SpaceSpec : SingleResultSpecification<Space>
{
    public static SpaceSpec ById(SpaceId id)
    {
        var spec = new SpaceSpec();
        spec.Query.Where(x => x.Id == id);
        return spec;
    }
}
