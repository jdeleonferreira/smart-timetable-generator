using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.Areas;

namespace SmartTimetableGenerator.Application.UseCases.Areas.Commands.AddSubject;

/// <summary>
/// Agrega una asignatura a un área. La intensidad horaria por grado se define después en el plan de estudios.
/// </summary>
public sealed record AddSubjectCommand(string Name, string? Code = null, int? Order = null) : IRequest<ErrorOr<Guid>>
{
    [JsonIgnore]
    public Guid AreaId { get; set; }
}

internal sealed class AddSubjectCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<AddSubjectCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(AddSubjectCommand request, CancellationToken cancellationToken)
    {
        var area = await dbContext.Areas
            .WithSpecification(AreaSpec.ById(AreaId.From(request.AreaId)))
            .FirstOrDefaultAsync(cancellationToken);
        if (area is null)
            return AreaErrors.NotFound;

        var subject = area.AddSubject(request.Name, request.Code, request.Order);
        if (subject.IsError)
            return subject.Errors;

        await dbContext.SaveChangesAsync(cancellationToken);
        return subject.Value.Id.Value;
    }
}

internal sealed class AddSubjectCommandValidator : AbstractValidator<AddSubjectCommand>
{
    public AddSubjectCommandValidator()
    {
        RuleFor(v => v.AreaId).NotEmpty();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(Subject.NameMaxLength);
        RuleFor(v => v.Code).MaximumLength(Subject.CodeMaxLength);
        RuleFor(v => v.Order).GreaterThanOrEqualTo(0);
    }
}
