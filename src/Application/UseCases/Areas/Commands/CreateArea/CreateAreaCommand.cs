using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.Areas;

namespace SmartTimetableGenerator.Application.UseCases.Areas.Commands.CreateArea;

/// <summary>
/// Crea un área de conocimiento. Sin orden, queda de última.
/// </summary>
public sealed record CreateAreaCommand(string Name, int? Order = null) : IRequest<ErrorOr<Guid>>;

internal sealed class CreateAreaCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateAreaCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(CreateAreaCommand request, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Areas.Select(a => new { a.Name, a.Order }).ToListAsync(cancellationToken);
        if (existing.Any(a => string.Equals(a.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return AreaErrors.DuplicateName;

        var order = request.Order ?? (existing.Count == 0 ? 0 : existing.Max(a => a.Order) + 1);
        var area = Area.Create(request.Name, order);
        dbContext.Areas.Add(area);
        await dbContext.SaveChangesAsync(cancellationToken);

        return area.Id.Value;
    }
}

internal sealed class CreateAreaCommandValidator : AbstractValidator<CreateAreaCommand>
{
    public CreateAreaCommandValidator()
    {
        RuleFor(v => v.Name).NotEmpty().MaximumLength(Area.NameMaxLength);
        RuleFor(v => v.Order).GreaterThanOrEqualTo(0);
    }
}
