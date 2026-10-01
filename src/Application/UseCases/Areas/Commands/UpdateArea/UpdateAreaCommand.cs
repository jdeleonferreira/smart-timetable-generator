using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Domain.Areas;

namespace SmartTimetableGenerator.Application.UseCases.Areas.Commands.UpdateArea;

/// <summary>
/// Cambia el nombre u orden de un área.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public sealed record UpdateAreaCommand(string Name, int Order) : IRequest<ErrorOr<Success>>
{
    [JsonIgnore]
    public Guid AreaId { get; set; }
}

internal sealed class UpdateAreaCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateAreaCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(UpdateAreaCommand request, CancellationToken cancellationToken)
    {
        var areaId = AreaId.From(request.AreaId);

        var area = await dbContext.Areas
            .WithSpecification(AreaSpec.ById(areaId))
            .FirstOrDefaultAsync(cancellationToken);
        if (area is null)
            return AreaErrors.NotFound;

        var others = await dbContext.Areas.Where(a => a.Id != areaId).Select(a => a.Name).ToListAsync(cancellationToken);
        if (others.Any(n => string.Equals(n, request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return AreaErrors.DuplicateName;

        area.Update(request.Name, request.Order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}

internal sealed class UpdateAreaCommandValidator : AbstractValidator<UpdateAreaCommand>
{
    public UpdateAreaCommandValidator()
    {
        RuleFor(v => v.AreaId).NotEmpty();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(Area.NameMaxLength);
        RuleFor(v => v.Order).GreaterThanOrEqualTo(0);
    }
}
