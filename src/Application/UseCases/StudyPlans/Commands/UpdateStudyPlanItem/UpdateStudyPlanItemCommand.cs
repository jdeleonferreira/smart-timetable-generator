using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Common;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.UpdateStudyPlanItem;

/// <summary>
/// Cambia la IH semanal y la forma de dictar una asignatura del plan.
/// </summary>
[Authorize(Roles = Roles.Managers)]
public sealed record UpdateStudyPlanItemCommand(
    DeliveryMode DeliveryMode,
    int WeeklyHours,
    Guid? TargetShiftId = null,
    Guid? IntegratedIntoSubjectId = null,
    string? Note = null) : IRequest<ErrorOr<Success>>
{
    [JsonIgnore]
    public Guid StudyPlanId { get; set; }

    [JsonIgnore]
    public Guid ItemId { get; set; }
}

internal sealed class UpdateStudyPlanItemCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<UpdateStudyPlanItemCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(UpdateStudyPlanItemCommand request, CancellationToken cancellationToken)
    {
        var plan = await StudyPlanReferences.LoadPlanAsync(dbContext, request.StudyPlanId, cancellationToken);
        if (plan is null)
            return StudyPlanErrors.NotFound;

        if (currentUser.EnsureCanManageCampus(plan.CampusId) is { } forbidden)
            return forbidden;

        var targetShiftId = request.TargetShiftId is { } shift ? ShiftId.From(shift) : (ShiftId?)null;
        var integratedInto = request.IntegratedIntoSubjectId is { } into ? SubjectId.From(into) : (SubjectId?)null;

        var delivery = await StudyPlanReferences.ValidateDeliveryAsync(dbContext, plan, targetShiftId, integratedInto, cancellationToken);
        if (delivery.IsError)
            return delivery.Errors;

        var result = plan.UpdateItem(StudyPlanItemId.From(request.ItemId), request.DeliveryMode, request.WeeklyHours,
            targetShiftId, integratedInto, request.Note);
        if (result.IsError)
            return result.Errors;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}

internal sealed class UpdateStudyPlanItemCommandValidator : AbstractValidator<UpdateStudyPlanItemCommand>
{
    public UpdateStudyPlanItemCommandValidator()
    {
        RuleFor(v => v.StudyPlanId).NotEmpty();
        RuleFor(v => v.ItemId).NotEmpty();
        RuleFor(v => v.DeliveryMode).IsInEnum();
        RuleFor(v => v.WeeklyHours).InclusiveBetween(0, StudyPlanItem.MaxWeeklyHours);
        RuleFor(v => v.TargetShiftId).NotEqual(Guid.Empty);
        RuleFor(v => v.IntegratedIntoSubjectId).NotEqual(Guid.Empty);
        RuleFor(v => v.Note).MaximumLength(StudyPlanItem.NoteMaxLength);
    }
}
