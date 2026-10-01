using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Common;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.RemoveStudyPlanItem;

/// <summary>
/// Quita una asignatura de un grado del plan.
/// </summary>
[Authorize(Roles = Roles.Managers)]
public sealed record RemoveStudyPlanItemCommand(Guid StudyPlanId, Guid ItemId) : IRequest<ErrorOr<Success>>;

internal sealed class RemoveStudyPlanItemCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<RemoveStudyPlanItemCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(RemoveStudyPlanItemCommand request, CancellationToken cancellationToken)
    {
        var plan = await StudyPlanReferences.LoadPlanAsync(dbContext, request.StudyPlanId, cancellationToken);
        if (plan is null)
            return StudyPlanErrors.NotFound;

        if (currentUser.EnsureCanManageCampus(plan.CampusId) is { } forbidden)
            return forbidden;

        var result = plan.RemoveItem(StudyPlanItemId.From(request.ItemId));
        if (result.IsError)
            return result.Errors;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
