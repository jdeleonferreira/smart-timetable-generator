using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Catalog.Queries;
using SmartTimetableGenerator.Domain.Teachers;

namespace SmartTimetableGenerator.Application.UseCases.Teachers.Commands.SetTeacherAvailability;

/// <summary>
/// Reemplaza la disponibilidad del docente: las franjas en que no puede dictar clase (<c>Unavailable</c>, restricción
/// que el generador siempre respeta) y las que prefiere evitar (<c>Avoid</c>, que el generador intenta respetar).
/// La puede editar el administrador, el coordinador de alguna de las sedes del docente y el propio docente.
/// </summary>
[Authorize(Roles = Roles.Admin + "," + Roles.Coordinator + "," + Roles.Teacher)]
public sealed record SetTeacherAvailabilityCommand(IReadOnlyList<AvailabilityRuleDto> Rules) : IRequest<ErrorOr<Success>>
{
    [JsonIgnore]
    public Guid TeacherId { get; set; }
}

internal sealed class SetTeacherAvailabilityCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<SetTeacherAvailabilityCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(SetTeacherAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var teacher = await dbContext.Teachers
            .WithSpecification(TeacherSpec.ById(TeacherId.From(request.TeacherId)))
            .FirstOrDefaultAsync(cancellationToken);
        if (teacher is null)
            return TeacherErrors.NotFound;

        var isOwnRecord = currentUser.TeacherId == request.TeacherId;
        var managesACampus = teacher.Campuses.Any(c => currentUser.CanManageCampus(c.CampusId));
        if (!currentUser.IsAdmin() && !isOwnRecord && !managesACampus)
            return SecurityErrors.Forbidden;

        var rules = request.Rules
            .Select(r => new AvailabilityRule(r.Day, r.Start, r.End, r.Kind))
            .ToList();

        var result = teacher.SetAvailability(rules);
        if (result.IsError)
            return result.Errors;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}

internal sealed class SetTeacherAvailabilityCommandValidator : AbstractValidator<SetTeacherAvailabilityCommand>
{
    public SetTeacherAvailabilityCommandValidator()
    {
        RuleFor(v => v.TeacherId).NotEmpty();
        RuleFor(v => v.Rules).NotNull();
        RuleForEach(v => v.Rules).ChildRules(rule =>
        {
            rule.RuleFor(r => r.End).GreaterThan(r => r.Start).WithMessage("La hora final debe ser posterior a la inicial");
            rule.RuleFor(r => r.Kind)
                .Must(k => k is AvailabilityKind.Unavailable or AvailabilityKind.Avoid)
                .WithMessage("Solo se admite 'no puede' o 'prefiere evitar'");
        });
    }
}
