using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.CreateStudyPlan;

/// <summary>
/// Crea el plan de estudios de una sede para un año lectivo: vacío o copiando otro plan
/// (normalmente el del año anterior, de esta u otra sede). Solo puede haber un plan por sede y año.
/// </summary>
/// <remarks>
/// Al copiar desde otra sede, las asignaturas en contrajornada se enlazan a la jornada de la nueva sede
/// que tenga el mismo nombre; si no existe, la copia no se hace.
/// </remarks>
public sealed record CreateStudyPlanCommand(
    Guid AcademicYearId,
    Guid CampusId,
    string? Name = null,
    Guid? CopyFromStudyPlanId = null) : IRequest<ErrorOr<Guid>>;

internal sealed class CreateStudyPlanCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateStudyPlanCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(CreateStudyPlanCommand request, CancellationToken cancellationToken)
    {
        var yearId = AcademicYearId.From(request.AcademicYearId);
        var campusId = CampusId.From(request.CampusId);

        var year = await dbContext.AcademicYears
            .WithSpecification(AcademicYearSpec.ById(yearId))
            .FirstOrDefaultAsync(cancellationToken);
        if (year is null)
            return AcademicYearErrors.NotFound;

        var campus = await dbContext.Campuses
            .WithSpecification(CampusSpec.ById(campusId))
            .FirstOrDefaultAsync(cancellationToken);
        if (campus is null)
            return CampusErrors.NotFound;

        if (await dbContext.StudyPlans.AnyAsync(p => p.AcademicYearId == yearId && p.CampusId == campusId, cancellationToken))
            return StudyPlanErrors.AlreadyExists;

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? $"Plan de estudios {year.Year} - {campus.Name}"
            : request.Name;

        StudyPlan plan;
        if (request.CopyFromStudyPlanId is null)
        {
            plan = StudyPlan.Create(yearId, campusId, name);
        }
        else
        {
            var copy = await CopyAsync(StudyPlanId.From(request.CopyFromStudyPlanId.Value), yearId, campus, name, cancellationToken);
            if (copy.IsError)
                return copy.Errors;
            plan = copy.Value;
        }

        dbContext.StudyPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        return plan.Id.Value;
    }

    private async Task<ErrorOr<StudyPlan>> CopyAsync(
        StudyPlanId sourceId,
        AcademicYearId yearId,
        Campus campus,
        string name,
        CancellationToken cancellationToken)
    {
        var source = await dbContext.StudyPlans
            .AsNoTracking()
            .WithSpecification(StudyPlanSpec.ById(sourceId))
            .FirstOrDefaultAsync(cancellationToken);
        if (source is null)
            return StudyPlanErrors.NotFound;

        var plan = StudyPlan.CreateCopy(source, yearId, campus.Id, name);
        if (source.CampusId == campus.Id)
            return plan;

        // Otra sede: las jornadas son otras. Se enlaza cada contrajornada con la jornada del mismo nombre.
        var sourceCampus = await dbContext.Campuses
            .AsNoTracking()
            .WithSpecification(CampusSpec.ById(source.CampusId))
            .FirstOrDefaultAsync(cancellationToken);

        foreach (var item in plan.Items.Where(i => i.DeliveryMode == DeliveryMode.CounterShift).ToList())
        {
            var shiftName = sourceCampus?.FindShift(item.TargetShiftId!.Value)?.Name;
            var target = campus.Shifts.FirstOrDefault(s => string.Equals(s.Name, shiftName, StringComparison.OrdinalIgnoreCase));
            if (target is null)
                return StudyPlanErrors.ShiftNotInCampus;

            var result = plan.UpdateItem(item.Id, item.DeliveryMode, item.WeeklyHours, target.Id, null, item.Note);
            if (result.IsError)
                return result.Errors;
        }

        return plan;
    }
}

internal sealed class CreateStudyPlanCommandValidator : AbstractValidator<CreateStudyPlanCommand>
{
    public CreateStudyPlanCommandValidator()
    {
        RuleFor(v => v.AcademicYearId).NotEmpty();
        RuleFor(v => v.CampusId).NotEmpty();
        RuleFor(v => v.Name).MaximumLength(StudyPlan.NameMaxLength);
        RuleFor(v => v.CopyFromStudyPlanId).NotEqual(Guid.Empty);
    }
}
