using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Spaces;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Application.UseCases.StudyPlans.Queries.GetStudyPlan;

/// <summary>
/// Plan de estudios completo, con la forma del documento institucional: áreas y asignaturas en filas,
/// grados en columnas (cada asignatura trae sus líneas por grado) y los totales semanales de cada grado,
/// generales y por periodo. Incluye las asignaturas activas aunque aún no estén en el plan, para poder llenarlas.
/// </summary>
public sealed record GetStudyPlanQuery(Guid StudyPlanId) : IRequest<ErrorOr<StudyPlanDto>>;

public sealed record StudyPlanDto(
    Guid Id,
    Guid AcademicYearId,
    int Year,
    Guid CampusId,
    string CampusName,
    string Name,
    StudyPlanStatus Status,
    DateTimeOffset? ApprovedAt,
    string? Notes,
    IReadOnlyList<StudyPlanPeriodDto> Periods,
    IReadOnlyList<StudyPlanShiftDto> Shifts,
    IReadOnlyList<StudyPlanGradeDto> Grades,
    IReadOnlyList<StudyPlanAreaDto> Areas);

public sealed record StudyPlanPeriodDto(Guid Id, int Number, string Name);

public sealed record StudyPlanShiftDto(Guid Id, string Name);

/// <summary>Columna de un grado con sus totales (regulares y en contrajornada).</summary>
public sealed record StudyPlanGradeDto(
    Guid Id,
    string Name,
    string ShortName,
    int Order,
    int WeeklyTotal,
    int CounterShiftTotal,
    IReadOnlyList<StudyPlanPeriodTotalDto> PeriodTotals);

public sealed record StudyPlanPeriodTotalDto(Guid AcademicPeriodId, int WeeklyTotal, int CounterShiftTotal);

public sealed record StudyPlanAreaDto(Guid Id, string Name, int Order, IReadOnlyList<StudyPlanSubjectDto> Subjects);

public sealed record StudyPlanSubjectDto(Guid Id, string Name, string? Code, int Order, bool IsActive, IReadOnlyList<StudyPlanItemDto> Items);

public sealed record StudyPlanItemDto(
    Guid Id,
    Guid GradeId,
    Guid SubjectId,
    DeliveryMode DeliveryMode,
    int WeeklyHours,
    Guid? TargetShiftId,
    Guid? IntegratedIntoSubjectId,
    string? Note,
    int? MaxHoursPerDay,
    int? MaxConsecutiveHours,
    SpaceType? RequiredSpaceType,
    IReadOnlyList<StudyPlanPeriodHoursDto> PeriodHours);

public sealed record StudyPlanPeriodHoursDto(Guid AcademicPeriodId, int WeeklyHours);

internal sealed class GetStudyPlanQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetStudyPlanQuery, ErrorOr<StudyPlanDto>>
{
    public async Task<ErrorOr<StudyPlanDto>> Handle(GetStudyPlanQuery request, CancellationToken cancellationToken)
    {
        var plan = await dbContext.StudyPlans
            .AsNoTracking()
            .WithSpecification(StudyPlanSpec.ById(StudyPlanId.From(request.StudyPlanId)))
            .FirstOrDefaultAsync(cancellationToken);
        if (plan is null)
            return StudyPlanErrors.NotFound;

        var year = await dbContext.AcademicYears
            .AsNoTracking()
            .WithSpecification(AcademicYearSpec.ById(plan.AcademicYearId))
            .FirstAsync(cancellationToken);
        var campus = await dbContext.Campuses
            .AsNoTracking()
            .WithSpecification(CampusSpec.ById(plan.CampusId))
            .FirstAsync(cancellationToken);
        var grades = await dbContext.Grades.AsNoTracking().ToListAsync(cancellationToken);
        var areas = await dbContext.Areas.AsNoTracking().Include(a => a.Subjects).ToListAsync(cancellationToken);

        var periods = year.Periods;
        var itemsBySubject = plan.Items.ToLookup(i => i.SubjectId);

        var gradeDtos = grades
            .OrderBy(g => g.Order).ThenBy(g => g.Name)
            .Select(g => new StudyPlanGradeDto(
                g.Id.Value, g.Name, g.ShortName, g.Order,
                plan.WeeklyTotal(g.Id),
                plan.WeeklyCounterShiftTotal(g.Id),
                periods
                    .Select(p => new StudyPlanPeriodTotalDto(p.Id.Value, plan.WeeklyTotal(g.Id, p.Id), plan.WeeklyCounterShiftTotal(g.Id, p.Id)))
                    .ToList()))
            .ToList();

        var areaDtos = areas
            .OrderBy(a => a.Order).ThenBy(a => a.Name)
            .Select(a => new StudyPlanAreaDto(a.Id.Value, a.Name, a.Order,
                a.Subjects
                    .Where(s => s.IsActive || itemsBySubject.Contains(s.Id))
                    .OrderBy(s => s.Order).ThenBy(s => s.Name)
                    .Select(s => new StudyPlanSubjectDto(s.Id.Value, s.Name, s.Code, s.Order, s.IsActive,
                        itemsBySubject[s.Id].Select(ToDto).ToList()))
                    .ToList()))
            .Where(a => a.Subjects.Count > 0)
            .ToList();

        return new StudyPlanDto(
            plan.Id.Value,
            year.Id.Value,
            year.Year,
            campus.Id.Value,
            campus.Name,
            plan.Name,
            plan.Status,
            plan.ApprovedAt,
            plan.Notes,
            periods.Select(p => new StudyPlanPeriodDto(p.Id.Value, p.Number, p.Name)).ToList(),
            campus.Shifts.OrderBy(s => s.Name).Select(s => new StudyPlanShiftDto(s.Id.Value, s.Name)).ToList(),
            gradeDtos,
            areaDtos);
    }

    private static StudyPlanItemDto ToDto(StudyPlanItem item) => new(
        item.Id.Value,
        item.GradeId.Value,
        item.SubjectId.Value,
        item.DeliveryMode,
        item.WeeklyHours,
        item.TargetShiftId?.Value,
        item.IntegratedIntoSubjectId?.Value,
        item.Note,
        item.MaxHoursPerDay,
        item.MaxConsecutiveHours,
        item.RequiredSpaceType,
        item.PeriodHours.Select(p => new StudyPlanPeriodHoursDto(p.AcademicPeriodId.Value, p.WeeklyHours)).ToList());
}
