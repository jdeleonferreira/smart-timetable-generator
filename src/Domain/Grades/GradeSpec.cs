namespace SmartTimetableGenerator.Domain.Grades;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class GradeSpec : SingleResultSpecification<Grade>
{
    public static GradeSpec ById(GradeId id)
    {
        var spec = new GradeSpec();
        spec.Query.Where(x => x.Id == id);
        return spec;
    }
}
