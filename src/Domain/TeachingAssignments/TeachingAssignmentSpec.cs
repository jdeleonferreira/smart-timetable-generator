namespace SmartTimetableGenerator.Domain.TeachingAssignments;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class TeachingAssignmentSpec : SingleResultSpecification<TeachingAssignment>
{
    public static TeachingAssignmentSpec ById(TeachingAssignmentId id)
    {
        var spec = new TeachingAssignmentSpec();
        spec.Query.Where(x => x.Id == id);
        return spec;
    }
}
