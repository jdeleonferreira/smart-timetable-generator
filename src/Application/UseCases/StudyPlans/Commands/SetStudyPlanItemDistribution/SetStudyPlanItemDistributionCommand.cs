using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Common;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.SetStudyPlanItemDistribution;

/// <summary>
/// Define cómo se reparten las horas de una asignatura en la semana: máximo por día, máximo seguidas
/// (2 = bloques dobles) y el tipo de espacio que requiere (ej.: sala de informática). Null = sin restricción propia.
/// </summary>
public sealed record SetStudyPlanItemDistributionCommand(
    int? MaxHoursPerDay,
    int? MaxConsecutiveHours,
    SpaceType? RequiredSpaceType) : IRequest<ErrorOr<Success>>
{
    [JsonIgnore]
    public Guid StudyPlanId { get; set; }

    [JsonIgnore]
    public Guid ItemId { get; set; }
}

internal sealed class SetStudyPlanItemDistributionCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SetStudyPlanItemDistributionCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(SetStudyPlanItemDistributionCommand request, CancellationToken cancellationToken)
    {
        var plan = await StudyPlanReferences.LoadPlanAsync(dbContext, request.StudyPlanId, cancellationToken);
        if (plan is null)
            return StudyPlanErrors.NotFound;

        var result = plan.SetItemDistribution(StudyPlanItemId.From(request.ItemId),
            request.MaxHoursPerDay, request.MaxConsecutiveHours, request.RequiredSpaceType);
        if (result.IsError)
            return result.Errors;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}

internal sealed class SetStudyPlanItemDistributionCommandValidator : AbstractValidator<SetStudyPlanItemDistributionCommand>
{
    public SetStudyPlanItemDistributionCommandValidator()
    {
        RuleFor(v => v.StudyPlanId).NotEmpty();
        RuleFor(v => v.ItemId).NotEmpty();
        RuleFor(v => v.RequiredSpaceType).IsInEnum();
    }
}
