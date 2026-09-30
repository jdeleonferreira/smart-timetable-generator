namespace SmartTimetableGenerator.Domain.Teachers;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class TeacherSpec : SingleResultSpecification<Teacher>
{
    public static TeacherSpec ById(TeacherId id)
    {
        var spec = new TeacherSpec();
        spec.Query.Where(x => x.Id == id);
        return spec;
    }
}
