using MediatR;
using SmartTimetableGenerator.Application.UseCases.Catalog.Queries;
using SmartTimetableGenerator.WebApi.Extensions;

namespace SmartTimetableGenerator.WebApi.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup("catalog");

        group
            .MapGet("/academic-years", async (ISender sender, CancellationToken ct) =>
                TypedResults.Ok(await sender.Send(new GetAcademicYearsQuery(), ct)))
            .WithName("GetAcademicYears")
            .WithSummary("Años lectivos con sus periodos")
            .ProducesGet<AcademicYearDto[]>();

        group
            .MapGet("/campuses", async (ISender sender, CancellationToken ct) =>
                TypedResults.Ok(await sender.Send(new GetCampusesQuery(), ct)))
            .WithName("GetCampuses")
            .WithSummary("Sedes con sus jornadas")
            .ProducesGet<CampusDto[]>();

        group
            .MapGet("/courses", async (ISender sender, Guid? academicYearId, Guid? campusId, CancellationToken ct) =>
                TypedResults.Ok(await sender.Send(new GetCoursesQuery(academicYearId, campusId), ct)))
            .WithName("GetCourses")
            .WithSummary("Cursos, opcionalmente por año lectivo y sede")
            .ProducesGet<CourseDto[]>();

        group
            .MapGet("/teachers", async (ISender sender, CancellationToken ct) =>
                TypedResults.Ok(await sender.Send(new GetTeachersQuery(), ct)))
            .WithName("GetTeachers")
            .WithSummary("Docentes con sus áreas y carga máxima")
            .ProducesGet<TeacherDto[]>();
    }
}
