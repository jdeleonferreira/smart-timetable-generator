namespace SmartTimetableGenerator.Domain.TimetableGeneration;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class GenerationJobSpec : SingleResultSpecification<GenerationJob>
{
    public static GenerationJobSpec ById(GenerationJobId id)
    {
        var spec = new GenerationJobSpec();
        spec.Query.Where(x => x.Id == id);
        return spec;
    }
}
