using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.Areas;

namespace SmartTimetableGenerator.Application.UseCases.Areas.Commands.UpdateSubject;

/// <summary>
/// Cambia el nombre, abreviatura, orden o estado (activa/inactiva) de una asignatura.
/// Las asignaturas no se eliminan porque pueden estar en planes de años anteriores; se desactivan.
/// </summary>
public sealed record UpdateSubjectCommand(string Name, string? Code, int Order, bool IsActive = true) : IRequest<ErrorOr<Success>>
{
    [JsonIgnore]
    public Guid AreaId { get; set; }

    [JsonIgnore]
    public Guid SubjectId { get; set; }
}

internal sealed class UpdateSubjectCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateSubjectCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(UpdateSubjectCommand request, CancellationToken cancellationToken)
    {
        var area = await dbContext.Areas
            .WithSpecification(AreaSpec.ById(AreaId.From(request.AreaId)))
            .FirstOrDefaultAsync(cancellationToken);
        if (area is null)
            return AreaErrors.NotFound;

        var subjectId = SubjectId.From(request.SubjectId);

        var updated = area.UpdateSubject(subjectId, request.Name, request.Code, request.Order);
        if (updated.IsError)
            return updated.Errors;

        var activated = area.SetSubjectActive(subjectId, request.IsActive);
        if (activated.IsError)
            return activated.Errors;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}

internal sealed class UpdateSubjectCommandValidator : AbstractValidator<UpdateSubjectCommand>
{
    public UpdateSubjectCommandValidator()
    {
        RuleFor(v => v.AreaId).NotEmpty();
        RuleFor(v => v.SubjectId).NotEmpty();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(Subject.NameMaxLength);
        RuleFor(v => v.Code).MaximumLength(Subject.CodeMaxLength);
        RuleFor(v => v.Order).GreaterThanOrEqualTo(0);
    }
}
