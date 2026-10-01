using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Teachers.Common;
using SmartTimetableGenerator.Domain.Teachers;

namespace SmartTimetableGenerator.Application.UseCases.Teachers.Commands.UpdateTeacher;

/// <summary>
/// Cambia los datos, las áreas, las sedes, la carga o el estado de un docente.
/// Los docentes no se eliminan (tienen clases en horarios); se desactivan y el generador deja de proponerlos.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public sealed record UpdateTeacherCommand(
    string FirstName,
    string LastName,
    int MaxWeeklyHours,
    IReadOnlyList<Guid> AreaIds,
    IReadOnlyList<Guid> CampusIds,
    string? Email = null,
    string? Phone = null,
    int? MaxDailyHours = null,
    int? MaxGapsPerDay = null,
    bool IsActive = true) : IRequest<ErrorOr<Success>>
{
    [JsonIgnore]
    public Guid TeacherId { get; set; }
}

internal sealed class UpdateTeacherCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateTeacherCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(UpdateTeacherCommand request, CancellationToken cancellationToken)
    {
        var teacher = await dbContext.Teachers
            .WithSpecification(TeacherSpec.ById(TeacherId.From(request.TeacherId)))
            .FirstOrDefaultAsync(cancellationToken);
        if (teacher is null)
            return TeacherErrors.NotFound;

        var applied = await TeacherReferences.ApplyAsync(dbContext, teacher, new TeacherData(
            request.FirstName, request.LastName, request.Email, request.Phone, request.MaxWeeklyHours,
            request.MaxDailyHours, request.MaxGapsPerDay, request.AreaIds, request.CampusIds), cancellationToken);
        if (applied.IsError)
            return applied.Errors;

        if (request.IsActive)
            teacher.Activate();
        else
            teacher.Deactivate();

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}

internal sealed class UpdateTeacherCommandValidator : AbstractValidator<UpdateTeacherCommand>
{
    public UpdateTeacherCommandValidator()
    {
        RuleFor(v => v.TeacherId).NotEmpty();
        RuleFor(v => v.FirstName).NotEmpty().MaximumLength(Teacher.NameMaxLength);
        RuleFor(v => v.LastName).NotEmpty().MaximumLength(Teacher.NameMaxLength);
        RuleFor(v => v.Email).EmailAddress().MaximumLength(Teacher.EmailMaxLength).When(v => !string.IsNullOrWhiteSpace(v.Email));
        RuleFor(v => v.Phone).MaximumLength(Teacher.PhoneMaxLength);
        RuleFor(v => v.MaxWeeklyHours).InclusiveBetween(1, Teacher.MaxAllowedWeeklyHours);
        RuleFor(v => v.AreaIds).NotNull();
        RuleFor(v => v.CampusIds).NotNull();
    }
}
