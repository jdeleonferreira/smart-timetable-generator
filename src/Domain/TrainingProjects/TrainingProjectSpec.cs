namespace SmartTimetableGenerator.Domain.TrainingProjects;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class TrainingProjectSpec : SingleResultSpecification<TrainingProject>
{
    public static TrainingProjectSpec ById(TrainingProjectId id)
    {
        var spec = new TrainingProjectSpec();
        spec.Query.Where(x => x.Id == id)
            .Include(x => x.Activities);
        return spec;
    }
}
