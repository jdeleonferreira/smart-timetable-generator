using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.Timetables;

namespace SmartTimetableGenerator.Application.UseCases.Timetables.Commands.PublishTimetable;

/// <summary>
/// Publica un horario en borrador para que docentes y cursos lo consulten.
/// </summary>
public sealed record PublishTimetableCommand(Guid TimetableId) : IRequest<ErrorOr<Success>>;

internal sealed class PublishTimetableCommandHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<PublishTimetableCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(PublishTimetableCommand request, CancellationToken cancellationToken)
    {
        var timetable = await dbContext.Timetables
            .WithSpecification(TimetableSpec.ById(TimetableId.From(request.TimetableId)))
            .FirstOrDefaultAsync(cancellationToken);
        if (timetable is null)
            return TimetableErrors.NotFound;

        var result = timetable.Publish(timeProvider.GetUtcNow());
        if (result.IsError)
            return result.Errors;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
