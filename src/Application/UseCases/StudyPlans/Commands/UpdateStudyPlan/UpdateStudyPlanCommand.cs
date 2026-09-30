using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Common;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.UpdateStudyPlan;

/// <summary>
/// Cambia el nombre y las notas generales del plan (se imprimen al pie del documento).
/// </summary>
[Authorize(Roles = Roles.Managers)]
public sealed record UpdateStudyPlanCommand(string Name, string? Notes) : IRequest<ErrorOr<Success>>
{
    [JsonIgnore]
    public Guid StudyPlanId { get; set; }
}

internal sealed class UpdateStudyPlanCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<UpdateStudyPlanCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(UpdateStudyPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await StudyPlanReferences.LoadPlanAsync(dbContext, request.StudyPlanId, cancellationToken);
        if (plan is null)
            return StudyPlanErrors.NotFound;

        if (currentUser.EnsureCanManageCampus(plan.CampusId) is { } forbidden)
            return forbidden;

        var result = plan.Update(request.Name, request.Notes);
        if (result.IsError)
            return result.Errors;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}

internal sealed class UpdateStudyPlanCommandValidator : AbstractValidator<UpdateStudyPlanCommand>
{
    public UpdateStudyPlanCommandValidator()
    {
        RuleFor(v => v.StudyPlanId).NotEmpty();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(StudyPlan.NameMaxLength);
        RuleFor(v => v.Notes).MaximumLength(StudyPlan.NotesMaxLength);
    }
}
