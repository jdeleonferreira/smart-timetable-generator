using MediatR;
using SmartTimetableGenerator.Application.UseCases.GenerationJobs.Queries.GetGenerationJob;
using SmartTimetableGenerator.Application.UseCases.Timetables.Commands.CreateTimetable;
using SmartTimetableGenerator.Application.UseCases.Timetables.Commands.GenerateTimetable;
using SmartTimetableGenerator.Application.UseCases.Timetables.Commands.PublishTimetable;
using SmartTimetableGenerator.Application.UseCases.Timetables.Queries.GetTimetableLessons;
using SmartTimetableGenerator.Application.UseCases.Timetables.Queries.GetTimetables;
using SmartTimetableGenerator.WebApi.Extensions;

namespace SmartTimetableGenerator.WebApi.Endpoints;

public static class TimetableEndpoints
{
    public static void MapTimetableEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup("timetables");

        group
            .MapGet("/", async (ISender sender, Guid? academicYearId, Guid? campusId, CancellationToken ct) =>
                TypedResults.Ok(await sender.Send(new GetTimetablesQuery(academicYearId, campusId), ct)))
            .WithName("GetTimetables")
            .WithSummary("Lista los horarios, opcionalmente por año lectivo y sede")
            .ProducesGet<TimetableSummaryDto[]>();

        group
            .MapPost("/", async (ISender sender, CreateTimetableCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.Match(id => Results.Created($"/api/timetables/{id}", new CreatedIdDto(id)), CustomResult.Problem);
            })
            .WithName("CreateTimetable")
            .WithSummary("Crea un horario en borrador para una sede y un periodo académico")
            .ProducesPost<CreatedIdDto>();

        group
            .MapPost("/{timetableId:guid}/generate", async (ISender sender, Guid timetableId, int? timeLimitSeconds, CancellationToken ct) =>
            {
                var command = new GenerateTimetableCommand(timeLimitSeconds ?? GenerateTimetableCommand.DefaultTimeLimitSeconds) { TimetableId = timetableId };
                var result = await sender.Send(command, ct);
                return result.Match(
                    jobId => Results.Accepted($"/api/generation-jobs/{jobId}", new GenerationQueuedDto(jobId)),
                    CustomResult.Problem);
            })
            .WithName("GenerateTimetable")
            .WithSummary("Pone en cola la generación del horario. Consulte el estado en /api/generation-jobs/{jobId}")
            .Produces<GenerationQueuedDto>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group
            .MapPost("/{timetableId:guid}/publish", async (ISender sender, Guid timetableId, CancellationToken ct) =>
            {
                var result = await sender.Send(new PublishTimetableCommand(timetableId), ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("PublishTimetable")
            .WithSummary("Publica el horario")
            .ProducesPut();

        group
            .MapGet("/{timetableId:guid}/lessons", async (
                ISender sender,
                Guid timetableId,
                Guid? courseId,
                Guid? teacherId,
                DayOfWeek? day,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetTimetableLessonsQuery(timetableId, courseId, teacherId, day), ct);
                return result.Match(lessons => Results.Ok(lessons), CustomResult.Problem);
            })
            .WithName("GetTimetableLessons")
            .WithSummary("Clases del horario. Filtre por courseId (vista por curso), teacherId (por docente) o day (institucional)")
            .ProducesGet<LessonDto[]>();

        app.MapApiGroup("generation-jobs")
            .MapGet("/{jobId:guid}", async (ISender sender, Guid jobId, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetGenerationJobQuery(jobId), ct);
                return result.Match(job => Results.Ok(job), CustomResult.Problem);
            })
            .WithName("GetGenerationJob")
            .WithSummary("Estado de una solicitud de generación")
            .ProducesGet<GenerationJobDto>();
    }
}
