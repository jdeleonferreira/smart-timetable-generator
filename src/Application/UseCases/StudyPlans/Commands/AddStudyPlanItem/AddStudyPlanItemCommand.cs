using System.Text.Json.Serialization;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Common;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.AddStudyPlanItem;

/// <summary>
/// Agrega una asignatura a un grado del plan con su intensidad horaria (IH) semanal y la forma de dictarla:
/// regular (en la jornada del curso), transversal (sin horas, integrada en otra asignatura)
/// o en contrajornada (en otra jornada de la sede).
/// </summary>
public sealed record AddStudyPlanItemCommand(
    Guid GradeId,
    Guid SubjectId,
    DeliveryMode DeliveryMode,
    int WeeklyHours,
    Guid? TargetShiftId = null,
    Guid? IntegratedIntoSubjectId = null,
    string? Note = null) : IRequest<ErrorOr<Guid>>
{
    [JsonIgnore]
    public Guid StudyPlanId { get; set; }
}

internal sealed class AddStudyPlanItemCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<AddStudyPlanItemCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(AddStudyPlanItemCommand request, CancellationToken cancellationToken)
    {
        var plan = await StudyPlanReferences.LoadPlanAsync(dbContext, request.StudyPlanId, cancellationToken);
        if (plan is null)
            return StudyPlanErrors.NotFound;

        var gradeId = GradeId.From(request.GradeId);
        var subjectId = SubjectId.From(request.SubjectId);
        var targetShiftId = request.TargetShiftId is { } shift ? ShiftId.From(shift) : (ShiftId?)null;
        var integratedInto = request.IntegratedIntoSubjectId is { } into ? SubjectId.From(into) : (SubjectId?)null;

        var references = await StudyPlanReferences.ValidateGradeAndSubjectAsync(dbContext, gradeId, subjectId, cancellationToken);
        if (references.IsError)
            return references.Errors;

        var delivery = await StudyPlanReferences.ValidateDeliveryAsync(dbContext, plan, targetShiftId, integratedInto, cancellationToken);
        if (delivery.IsError)
            return delivery.Errors;

        var item = plan.AddItem(gradeId, subjectId, request.DeliveryMode, request.WeeklyHours, targetShiftId, integratedInto, request.Note);
        if (item.IsError)
            return item.Errors;

        await dbContext.SaveChangesAsync(cancellationToken);
        return item.Value.Id.Value;
    }
}

internal sealed class AddStudyPlanItemCommandValidator : AbstractValidator<AddStudyPlanItemCommand>
{
    public AddStudyPlanItemCommandValidator()
    {
        RuleFor(v => v.StudyPlanId).NotEmpty();
        RuleFor(v => v.GradeId).NotEmpty();
        RuleFor(v => v.SubjectId).NotEmpty();
        RuleFor(v => v.DeliveryMode).IsInEnum();
        RuleFor(v => v.WeeklyHours).InclusiveBetween(0, StudyPlanItem.MaxWeeklyHours);
        RuleFor(v => v.TargetShiftId).NotEqual(Guid.Empty);
        RuleFor(v => v.IntegratedIntoSubjectId).NotEqual(Guid.Empty);
        RuleFor(v => v.Note).MaximumLength(StudyPlanItem.NoteMaxLength);
    }
}
