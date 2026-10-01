using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Teachers.Common;
using SmartTimetableGenerator.Domain.Teachers;

namespace SmartTimetableGenerator.Application.UseCases.Teachers.Commands.CreateTeacher;

/// <summary>
/// Registra un docente con las áreas que puede dictar, las sedes donde trabaja y su carga máxima.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public sealed record CreateTeacherCommand(
    string FirstName,
    string LastName,
    int MaxWeeklyHours,
    IReadOnlyList<Guid> AreaIds,
    IReadOnlyList<Guid> CampusIds,
    string? Email = null,
    string? Phone = null,
    int? MaxDailyHours = null,
    int? MaxGapsPerDay = null) : IRequest<ErrorOr<Guid>>;

internal sealed class CreateTeacherCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateTeacherCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(CreateTeacherCommand request, CancellationToken cancellationToken)
    {
        var teacher = Teacher.Create(request.FirstName, request.LastName, Math.Clamp(request.MaxWeeklyHours, 1, Teacher.MaxAllowedWeeklyHours));

        var applied = await TeacherReferences.ApplyAsync(dbContext, teacher, new TeacherData(
            request.FirstName, request.LastName, request.Email, request.Phone, request.MaxWeeklyHours,
            request.MaxDailyHours, request.MaxGapsPerDay, request.AreaIds, request.CampusIds), cancellationToken);
        if (applied.IsError)
            return applied.Errors;

        dbContext.Teachers.Add(teacher);
        await dbContext.SaveChangesAsync(cancellationToken);
        return teacher.Id.Value;
    }
}

internal sealed class CreateTeacherCommandValidator : AbstractValidator<CreateTeacherCommand>
{
    public CreateTeacherCommandValidator()
    {
        RuleFor(v => v.FirstName).NotEmpty().MaximumLength(Teacher.NameMaxLength);
        RuleFor(v => v.LastName).NotEmpty().MaximumLength(Teacher.NameMaxLength);
        RuleFor(v => v.Email).EmailAddress().MaximumLength(Teacher.EmailMaxLength).When(v => !string.IsNullOrWhiteSpace(v.Email));
        RuleFor(v => v.Phone).MaximumLength(Teacher.PhoneMaxLength);
        RuleFor(v => v.MaxWeeklyHours).InclusiveBetween(1, Teacher.MaxAllowedWeeklyHours);
        RuleFor(v => v.AreaIds).NotNull();
        RuleFor(v => v.CampusIds).NotNull();
    }
}
