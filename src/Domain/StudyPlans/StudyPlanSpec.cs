namespace SmartTimetableGenerator.Domain.StudyPlans;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class StudyPlanSpec : SingleResultSpecification<StudyPlan>
{
    public static StudyPlanSpec ById(StudyPlanId id)
    {
        var spec = new StudyPlanSpec();
        spec.Query.Where(x => x.Id == id)
            .Include(x => x.Items);
        return spec;
    }

    public static StudyPlanSpec ByYearAndCampus(AcademicYears.AcademicYearId academicYearId, Campuses.CampusId campusId)
    {
        var spec = new StudyPlanSpec();
        spec.Query
            .Where(x => x.AcademicYearId == academicYearId && x.CampusId == campusId)
            .Include(x => x.Items);
        return spec;
    }
}
