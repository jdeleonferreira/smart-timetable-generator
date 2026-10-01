namespace SmartTimetableGenerator.Domain.Courses;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class CourseSpec : SingleResultSpecification<Course>
{
    public static CourseSpec ById(CourseId id)
    {
        var spec = new CourseSpec();
        spec.Query.Where(x => x.Id == id);
        return spec;
    }
}
