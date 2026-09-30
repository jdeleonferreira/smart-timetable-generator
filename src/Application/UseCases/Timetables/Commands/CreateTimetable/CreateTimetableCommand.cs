using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.Timetables;

namespace SmartTimetableGenerator.Application.UseCases.Timetables.Commands.CreateTimetable;

/// <summary>
/// Crea un horario vacío (borrador) para una sede y periodo, ligado al plan de estudios de la sede en ese año.
/// </summary>
[Authorize(Roles = Roles.Managers)]
public sealed record CreateTimetableCommand(
    Guid AcademicYearId,
    Guid CampusId,
    Guid AcademicPeriodId,
    string? Name) : IRequest<ErrorOr<Guid>>;

internal sealed class CreateTimetableCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<CreateTimetableCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(CreateTimetableCommand request, CancellationToken cancellationToken)
    {
        var yearId = AcademicYearId.From(request.AcademicYearId);
        var campusId = CampusId.From(request.CampusId);
        var periodId = AcademicPeriodId.From(request.AcademicPeriodId);

        var year = await dbContext.AcademicYears
            .WithSpecification(AcademicYearSpec.ById(yearId))
            .FirstOrDefaultAsync(cancellationToken);
        if (year is null)
            return AcademicYearErrors.NotFound;

        var period = year.FindPeriod(periodId);
        if (period is null)
            return AcademicYearErrors.PeriodNotFound;

        var campus = await dbContext.Campuses
            .WithSpecification(CampusSpec.ById(campusId))
            .FirstOrDefaultAsync(cancellationToken);
        if (campus is null)
            return CampusErrors.NotFound;

        if (currentUser.EnsureCanManageCampus(campusId) is { } forbidden)
            return forbidden;

        var plan = await dbContext.StudyPlans
            .WithSpecification(StudyPlanSpec.ByYearAndCampus(yearId, campusId))
            .FirstOrDefaultAsync(cancellationToken);
        if (plan is null)
            return StudyPlanErrors.NotFound;

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? $"Horario {campus.Name} - {period.Name} {year.Year}"
            : request.Name;

        var timetable = Timetable.Create(yearId, campusId, periodId, plan.Id, name);
        dbContext.Timetables.Add(timetable);
        await dbContext.SaveChangesAsync(cancellationToken);

        return timetable.Id.Value;
    }
}

internal sealed class CreateTimetableCommandValidator : AbstractValidator<CreateTimetableCommand>
{
    public CreateTimetableCommandValidator()
    {
        RuleFor(v => v.AcademicYearId).NotEmpty();
        RuleFor(v => v.CampusId).NotEmpty();
        RuleFor(v => v.AcademicPeriodId).NotEmpty();
        RuleFor(v => v.Name).MaximumLength(Timetable.NameMaxLength);
    }
}
