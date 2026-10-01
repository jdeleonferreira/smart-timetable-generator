using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Common;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.SetStudyPlanItemPeriodHours;

/// <summary>
/// Fija una IH semanal distinta para un periodo académico. WeeklyHours = null quita el ajuste
/// y el periodo vuelve a la IH general de la asignatura.
/// </summary>
[Authorize(Roles = Roles.Managers)]
public sealed record SetStudyPlanItemPeriodHoursCommand(int? WeeklyHours) : IRequest<ErrorOr<Success>>
{
    [JsonIgnore]
    public Guid StudyPlanId { get; set; }

    [JsonIgnore]
    public Guid ItemId { get; set; }

    [JsonIgnore]
    public Guid AcademicPeriodId { get; set; }
}

internal sealed class SetStudyPlanItemPeriodHoursCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<SetStudyPlanItemPeriodHoursCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(SetStudyPlanItemPeriodHoursCommand request, CancellationToken cancellationToken)
    {
        var plan = await StudyPlanReferences.LoadPlanAsync(dbContext, request.StudyPlanId, cancellationToken);
        if (plan is null)
            return StudyPlanErrors.NotFound;

        if (currentUser.EnsureCanManageCampus(plan.CampusId) is { } forbidden)
            return forbidden;

        var periodId = AcademicPeriodId.From(request.AcademicPeriodId);
        var year = await dbContext.AcademicYears
            .WithSpecification(AcademicYearSpec.ById(plan.AcademicYearId))
            .FirstOrDefaultAsync(cancellationToken);
        if (year?.FindPeriod(periodId) is null)
            return StudyPlanErrors.PeriodNotInYear;

        var result = plan.SetItemPeriodHours(StudyPlanItemId.From(request.ItemId), periodId, request.WeeklyHours);
        if (result.IsError)
            return result.Errors;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}

internal sealed class SetStudyPlanItemPeriodHoursCommandValidator : AbstractValidator<SetStudyPlanItemPeriodHoursCommand>
{
    public SetStudyPlanItemPeriodHoursCommandValidator()
    {
        RuleFor(v => v.StudyPlanId).NotEmpty();
        RuleFor(v => v.ItemId).NotEmpty();
        RuleFor(v => v.AcademicPeriodId).NotEmpty();
        RuleFor(v => v.WeeklyHours).InclusiveBetween(0, StudyPlanItem.MaxWeeklyHours);
    }
}
