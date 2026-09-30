using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.Grades;

namespace SmartTimetableGenerator.Application.UseCases.Grades.Commands.CreateGrade;

/// <summary>
/// Agrega un grado al catálogo institucional.
/// </summary>
public sealed record CreateGradeCommand(string Name, string ShortName, EducationLevel Level, int Order) : IRequest<ErrorOr<Guid>>;

internal sealed class CreateGradeCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateGradeCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(CreateGradeCommand request, CancellationToken cancellationToken)
    {
        var names = await dbContext.Grades.Select(g => g.Name).ToListAsync(cancellationToken);
        if (names.Any(n => string.Equals(n, request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return GradeErrors.DuplicateName;

        var grade = Grade.Create(request.Name, request.ShortName, request.Level, request.Order);
        dbContext.Grades.Add(grade);
        await dbContext.SaveChangesAsync(cancellationToken);

        return grade.Id.Value;
    }
}

internal sealed class CreateGradeCommandValidator : AbstractValidator<CreateGradeCommand>
{
    public CreateGradeCommandValidator()
    {
        RuleFor(v => v.Name).NotEmpty().MaximumLength(Grade.NameMaxLength);
        RuleFor(v => v.ShortName).NotEmpty().MaximumLength(Grade.ShortNameMaxLength);
        RuleFor(v => v.Level).IsInEnum();
        RuleFor(v => v.Order).GreaterThanOrEqualTo(0);
    }
}
