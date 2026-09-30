using MediatR;
using SmartTimetableGenerator.Application.UseCases.Areas.Commands.AddSubject;
using SmartTimetableGenerator.Application.UseCases.Areas.Commands.CreateArea;
using SmartTimetableGenerator.Application.UseCases.Areas.Commands.UpdateArea;
using SmartTimetableGenerator.Application.UseCases.Areas.Commands.UpdateSubject;
using SmartTimetableGenerator.Application.UseCases.Areas.Queries.GetAreas;
using SmartTimetableGenerator.Application.UseCases.Grades.Commands.CreateGrade;
using SmartTimetableGenerator.Application.UseCases.Grades.Commands.UpdateGrade;
using SmartTimetableGenerator.Application.UseCases.Grades.Queries.GetGrades;
using SmartTimetableGenerator.WebApi.Extensions;

namespace SmartTimetableGenerator.WebApi.Endpoints;

/// <summary>
/// Catálogo institucional del plan de estudios: áreas con sus asignaturas, y grados.
/// </summary>
public static class AreaEndpoints
{
    public static void MapAreaAndGradeEndpoints(this WebApplication app)
    {
        var areas = app.MapApiGroup("areas");

        areas
            .MapGet("/", async (ISender sender, bool? includeInactiveSubjects, CancellationToken ct) =>
                TypedResults.Ok(await sender.Send(new GetAreasQuery(includeInactiveSubjects ?? true), ct)))
            .WithName("GetAreas")
            .WithSummary("Áreas de conocimiento con sus asignaturas")
            .ProducesGet<AreaDto[]>();

        areas
            .MapPost("/", async (ISender sender, CreateAreaCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.Match(id => Results.Created($"/api/areas/{id}", new { id }), CustomResult.Problem);
            })
            .WithName("CreateArea")
            .WithSummary("Crea un área de conocimiento")
            .ProducesPost()
            .ProducesProblem(StatusCodes.Status409Conflict);

        areas
            .MapPut("/{areaId:guid}", async (ISender sender, Guid areaId, UpdateAreaCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command with { AreaId = areaId }, ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("UpdateArea")
            .WithSummary("Cambia el nombre u orden de un área")
            .ProducesPut()
            .ProducesProblem(StatusCodes.Status409Conflict);

        areas
            .MapPost("/{areaId:guid}/subjects", async (ISender sender, Guid areaId, AddSubjectCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command with { AreaId = areaId }, ct);
                return result.Match(id => Results.Created($"/api/areas/{areaId}/subjects/{id}", new { id }), CustomResult.Problem);
            })
            .WithName("AddSubject")
            .WithSummary("Agrega una asignatura al área")
            .ProducesPost()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        areas
            .MapPut("/{areaId:guid}/subjects/{subjectId:guid}", async (
                ISender sender, Guid areaId, Guid subjectId, UpdateSubjectCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command with { AreaId = areaId, SubjectId = subjectId }, ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("UpdateSubject")
            .WithSummary("Cambia una asignatura; isActive = false la desactiva (no se borran)")
            .ProducesPut()
            .ProducesProblem(StatusCodes.Status409Conflict);

        var grades = app.MapApiGroup("grades");

        grades
            .MapGet("/", async (ISender sender, CancellationToken ct) =>
                TypedResults.Ok(await sender.Send(new GetGradesQuery(), ct)))
            .WithName("GetGrades")
            .WithSummary("Grados del catálogo institucional")
            .ProducesGet<GradeDto[]>();

        grades
            .MapPost("/", async (ISender sender, CreateGradeCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.Match(id => Results.Created($"/api/grades/{id}", new { id }), CustomResult.Problem);
            })
            .WithName("CreateGrade")
            .WithSummary("Agrega un grado (Level: Preschool, Primary, LowerSecondary, UpperSecondary)")
            .ProducesPost()
            .ProducesProblem(StatusCodes.Status409Conflict);

        grades
            .MapPut("/{gradeId:guid}", async (ISender sender, Guid gradeId, UpdateGradeCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command with { GradeId = gradeId }, ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("UpdateGrade")
            .WithSummary("Cambia un grado")
            .ProducesPut()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
