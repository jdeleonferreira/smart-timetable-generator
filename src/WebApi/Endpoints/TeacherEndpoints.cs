using MediatR;
using SmartTimetableGenerator.Application.UseCases.Teachers.Commands.CreateTeacher;
using SmartTimetableGenerator.Application.UseCases.Teachers.Commands.SetTeacherAvailability;
using SmartTimetableGenerator.Application.UseCases.Teachers.Commands.SetTeachingAssignment;
using SmartTimetableGenerator.Application.UseCases.Teachers.Commands.UpdateTeacher;
using SmartTimetableGenerator.Application.UseCases.Teachers.Queries.GetTeachingAssignments;
using SmartTimetableGenerator.WebApi.Extensions;

namespace SmartTimetableGenerator.WebApi.Endpoints;

/// <summary>
/// Gestión de docentes y de la asignación académica (qué docente dicta cada asignatura en cada curso).
/// La lista de docentes está en GET /api/catalog/teachers.
/// </summary>
public static class TeacherEndpoints
{
    public static void MapTeacherEndpoints(this WebApplication app)
    {
        var teachers = app.MapApiGroup("teachers");

        teachers
            .MapPost("/", async (ISender sender, CreateTeacherCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.Match(id => Results.Created($"/api/teachers/{id}", new CreatedIdDto(id)), CustomResult.Problem);
            })
            .WithName("CreateTeacher")
            .WithSummary("Registra un docente con sus áreas, sedes y carga máxima")
            .ProducesPost<CreatedIdDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        teachers
            .MapPut("/{teacherId:guid}", async (ISender sender, Guid teacherId, UpdateTeacherCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command with { TeacherId = teacherId }, ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("UpdateTeacher")
            .WithSummary("Cambia un docente; isActive = false lo desactiva (no se borran)")
            .ProducesPut()
            .ProducesProblem(StatusCodes.Status409Conflict);

        teachers
            .MapPut("/{teacherId:guid}/availability", async (ISender sender, Guid teacherId, SetTeacherAvailabilityCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command with { TeacherId = teacherId }, ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("SetTeacherAvailability")
            .WithSummary("Reemplaza las franjas en que el docente no puede (Unavailable) o prefiere evitar (Avoid) dictar clase")
            .ProducesPut();

        var assignments = app.MapApiGroup("teaching-assignments");

        assignments
            .MapGet("/", async (ISender sender, Guid academicYearId, Guid campusId, CancellationToken ct) =>
                TypedResults.Ok(await sender.Send(new GetTeachingAssignmentsQuery(academicYearId, campusId), ct)))
            .WithName("GetTeachingAssignments")
            .WithSummary("Docente de cada asignatura por curso, para una sede y año lectivo")
            .ProducesGet<TeachingAssignmentDto[]>();

        assignments
            .MapPut("/courses/{courseId:guid}/subjects/{subjectId:guid}", async (
                ISender sender, Guid courseId, Guid subjectId, SetTeachingAssignmentBody body, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetTeachingAssignmentCommand(courseId, subjectId, body.TeacherId), ct);
                return result.Match(value => Results.Ok(value), CustomResult.Problem);
            })
            .WithName("SetTeachingAssignment")
            .WithSummary("Fija el docente de una asignatura en un curso (teacherId nulo lo libera) y actualiza los horarios en borrador")
            .Produces<SetTeachingAssignmentResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

/// <summary>Docente a asignar; nulo libera la asignación.</summary>
public sealed record SetTeachingAssignmentBody(Guid? TeacherId);
