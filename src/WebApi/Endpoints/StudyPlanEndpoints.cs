using MediatR;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.AddStudyPlanItem;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.ApproveStudyPlan;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.CreateStudyPlan;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.RemoveStudyPlanItem;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.SetStudyPlanItemDistribution;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.SetStudyPlanItemPeriodHours;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.UpdateStudyPlan;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.UpdateStudyPlanItem;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Queries.GetStudyPlan;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Queries.GetStudyPlans;
using SmartTimetableGenerator.WebApi.Extensions;

namespace SmartTimetableGenerator.WebApi.Endpoints;

public static class StudyPlanEndpoints
{
    public static void MapStudyPlanEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup("study-plans");

        group
            .MapGet("/", async (ISender sender, Guid? academicYearId, Guid? campusId, CancellationToken ct) =>
                TypedResults.Ok(await sender.Send(new GetStudyPlansQuery(academicYearId, campusId), ct)))
            .WithName("GetStudyPlans")
            .WithSummary("Planes de estudio, opcionalmente por año lectivo y sede")
            .ProducesGet<StudyPlanSummaryDto[]>();

        group
            .MapGet("/{studyPlanId:guid}", async (ISender sender, Guid studyPlanId, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetStudyPlanQuery(studyPlanId), ct);
                return result.Match(plan => Results.Ok(plan), CustomResult.Problem);
            })
            .WithName("GetStudyPlan")
            .WithSummary("Plan completo: áreas y asignaturas (filas), grados (columnas), IH y totales por grado y periodo")
            .ProducesGet<StudyPlanDto>();

        group
            .MapPost("/", async (ISender sender, CreateStudyPlanCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.Match(id => Results.Created($"/api/study-plans/{id}", new { id }), CustomResult.Problem);
            })
            .WithName("CreateStudyPlan")
            .WithSummary("Crea el plan de una sede y año lectivo, vacío o copiando otro plan (copyFromStudyPlanId)")
            .ProducesPost()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group
            .MapPut("/{studyPlanId:guid}", async (ISender sender, Guid studyPlanId, UpdateStudyPlanCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command with { StudyPlanId = studyPlanId }, ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("UpdateStudyPlan")
            .WithSummary("Cambia el nombre y las notas del plan")
            .ProducesPut()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group
            .MapPost("/{studyPlanId:guid}/approve", async (ISender sender, Guid studyPlanId, CancellationToken ct) =>
            {
                var result = await sender.Send(new ApproveStudyPlanCommand(studyPlanId), ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("ApproveStudyPlan")
            .WithSummary("Aprueba el plan; queda cerrado a cambios hasta que se reabra")
            .ProducesPut()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group
            .MapPost("/{studyPlanId:guid}/reopen", async (ISender sender, Guid studyPlanId, CancellationToken ct) =>
            {
                var result = await sender.Send(new ReopenStudyPlanCommand(studyPlanId), ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("ReopenStudyPlan")
            .WithSummary("Reabre un plan aprobado para modificarlo")
            .ProducesPut();

        group
            .MapPost("/{studyPlanId:guid}/items", async (ISender sender, Guid studyPlanId, AddStudyPlanItemCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command with { StudyPlanId = studyPlanId }, ct);
                return result.Match(
                    id => Results.Created($"/api/study-plans/{studyPlanId}/items/{id}", new { id }),
                    CustomResult.Problem);
            })
            .WithName("AddStudyPlanItem")
            .WithSummary("Agrega una asignatura a un grado con su IH y forma de dictarla (Regular, Transversal, CounterShift)")
            .ProducesPost()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group
            .MapPut("/{studyPlanId:guid}/items/{itemId:guid}", async (
                ISender sender, Guid studyPlanId, Guid itemId, UpdateStudyPlanItemCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command with { StudyPlanId = studyPlanId, ItemId = itemId }, ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("UpdateStudyPlanItem")
            .WithSummary("Cambia la IH y la forma de dictar una asignatura del plan")
            .ProducesPut()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group
            .MapPut("/{studyPlanId:guid}/items/{itemId:guid}/distribution", async (
                ISender sender, Guid studyPlanId, Guid itemId, SetStudyPlanItemDistributionCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(command with { StudyPlanId = studyPlanId, ItemId = itemId }, ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("SetStudyPlanItemDistribution")
            .WithSummary("Máximo de horas por día, máximo seguidas (2 = bloques dobles) y tipo de espacio requerido")
            .ProducesPut()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group
            .MapPut("/{studyPlanId:guid}/items/{itemId:guid}/periods/{academicPeriodId:guid}", async (
                ISender sender, Guid studyPlanId, Guid itemId, Guid academicPeriodId, SetStudyPlanItemPeriodHoursCommand command, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    command with { StudyPlanId = studyPlanId, ItemId = itemId, AcademicPeriodId = academicPeriodId }, ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("SetStudyPlanItemPeriodHours")
            .WithSummary("IH distinta en un periodo; weeklyHours = null vuelve a la IH general")
            .ProducesPut()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group
            .MapDelete("/{studyPlanId:guid}/items/{itemId:guid}", async (ISender sender, Guid studyPlanId, Guid itemId, CancellationToken ct) =>
            {
                var result = await sender.Send(new RemoveStudyPlanItemCommand(studyPlanId, itemId), ct);
                return result.Match(_ => Results.NoContent(), CustomResult.Problem);
            })
            .WithName("RemoveStudyPlanItem")
            .WithSummary("Quita una asignatura de un grado del plan")
            .ProducesDelete()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
