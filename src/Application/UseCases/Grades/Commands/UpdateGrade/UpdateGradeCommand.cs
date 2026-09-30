using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Domain.Grades;

namespace SmartTimetableGenerator.Application.UseCases.Grades.Commands.UpdateGrade;

/// <summary>
/// Cambia el nombre, nombre corto, nivel u orden de un grado.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public sealed record UpdateGradeCommand(string Name, string ShortName, EducationLevel Level, int Order) : IRequest<ErrorOr<Success>>
{
    [JsonIgnore]
    public Guid GradeId { get; set; }
}

internal sealed class UpdateGradeCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateGradeCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(UpdateGradeCommand request, CancellationToken cancellationToken)
    {
        var gradeId = GradeId.From(request.GradeId);

        var grade = await dbContext.Grades
            .WithSpecification(GradeSpec.ById(gradeId))
            .FirstOrDefaultAsync(cancellationToken);
        if (grade is null)
            return GradeErrors.NotFound;

        var others = await dbContext.Grades.Where(g => g.Id != gradeId).Select(g => g.Name).ToListAsync(cancellationToken);
        if (others.Any(n => string.Equals(n, request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return GradeErrors.DuplicateName;

        grade.Update(request.Name, request.ShortName, request.Level, request.Order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}

internal sealed class UpdateGradeCommandValidator : AbstractValidator<UpdateGradeCommand>
{
    public UpdateGradeCommandValidator()
    {
        RuleFor(v => v.GradeId).NotEmpty();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(Grade.NameMaxLength);
        RuleFor(v => v.ShortName).NotEmpty().MaximumLength(Grade.ShortNameMaxLength);
        RuleFor(v => v.Level).IsInEnum();
        RuleFor(v => v.Order).GreaterThanOrEqualTo(0);
    }
}
