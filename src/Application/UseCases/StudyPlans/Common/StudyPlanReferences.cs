using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.StudyPlans;

namespace SmartTimetableGenerator.Application.UseCases.StudyPlans.Common;

/// <summary>
/// Verifica que lo que se referencia desde una línea del plan exista y sea coherente con el plan
/// (grado y asignatura del catálogo, jornada de la misma sede, asignatura donde se integra).
/// </summary>
internal static class StudyPlanReferences
{
    public static async Task<StudyPlan?> LoadPlanAsync(IApplicationDbContext dbContext, Guid planId, CancellationToken cancellationToken) =>
        await dbContext.StudyPlans
            .WithSpecification(StudyPlanSpec.ById(StudyPlanId.From(planId)))
            .FirstOrDefaultAsync(cancellationToken);

    public static async Task<ErrorOr<Success>> ValidateGradeAndSubjectAsync(
        IApplicationDbContext dbContext,
        GradeId gradeId,
        SubjectId subjectId,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Grades.AnyAsync(g => g.Id == gradeId, cancellationToken))
            return GradeErrors.NotFound;

        var subject = await FindSubjectAsync(dbContext, subjectId, cancellationToken);
        if (subject is null)
            return AreaErrors.SubjectNotFound;

        if (!subject.IsActive)
            return StudyPlanErrors.SubjectInactive;

        return Result.Success;
    }

    public static async Task<ErrorOr<Success>> ValidateDeliveryAsync(
        IApplicationDbContext dbContext,
        StudyPlan plan,
        ShiftId? targetShiftId,
        SubjectId? integratedIntoSubjectId,
        CancellationToken cancellationToken)
    {
        if (targetShiftId is not null)
        {
            var campus = await dbContext.Campuses
                .WithSpecification(CampusSpec.ById(plan.CampusId))
                .FirstOrDefaultAsync(cancellationToken);
            if (campus?.FindShift(targetShiftId.Value) is null)
                return StudyPlanErrors.ShiftNotInCampus;
        }

        if (integratedIntoSubjectId is not null &&
            await FindSubjectAsync(dbContext, integratedIntoSubjectId.Value, cancellationToken) is null)
        {
            return AreaErrors.SubjectNotFound;
        }

        return Result.Success;
    }

    private static async Task<Subject?> FindSubjectAsync(IApplicationDbContext dbContext, SubjectId subjectId, CancellationToken cancellationToken)
    {
        var area = await dbContext.Areas
            .WithSpecification(AreaSpec.BySubject(subjectId))
            .FirstOrDefaultAsync(cancellationToken);
        return area?.FindSubject(subjectId);
    }
}
