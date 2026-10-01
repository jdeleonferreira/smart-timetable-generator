using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Common;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.ApproveStudyPlan;

/// <summary>
/// Aprueba el plan: queda cerrado a cambios hasta que se reabra.
/// </summary>
[Authorize(Roles = Roles.Managers)]
public sealed record ApproveStudyPlanCommand(Guid StudyPlanId) : IRequest<ErrorOr<Success>>;

internal sealed class ApproveStudyPlanCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, TimeProvider timeProvider)
    : IRequestHandler<ApproveStudyPlanCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(ApproveStudyPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await StudyPlanReferences.LoadPlanAsync(dbContext, request.StudyPlanId, cancellationToken);
        if (plan is null)
            return StudyPlanErrors.NotFound;

        if (currentUser.EnsureCanManageCampus(plan.CampusId) is { } forbidden)
            return forbidden;

        var result = plan.Approve(timeProvider.GetUtcNow());
        if (result.IsError)
            return result.Errors;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}

/// <summary>
/// Reabre un plan aprobado para poder modificarlo.
/// </summary>
[Authorize(Roles = Roles.Managers)]
public sealed record ReopenStudyPlanCommand(Guid StudyPlanId) : IRequest<ErrorOr<Success>>;

internal sealed class ReopenStudyPlanCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<ReopenStudyPlanCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(ReopenStudyPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await StudyPlanReferences.LoadPlanAsync(dbContext, request.StudyPlanId, cancellationToken);
        if (plan is null)
            return StudyPlanErrors.NotFound;

        if (currentUser.EnsureCanManageCampus(plan.CampusId) is { } forbidden)
            return forbidden;

        plan.Reopen();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
