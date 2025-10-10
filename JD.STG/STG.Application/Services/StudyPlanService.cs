using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;
public sealed class StudyPlanService
{
    private readonly IStudyPlanRepository _studyPlan;
    private readonly IUnitOfWork _uow;

    public StudyPlanService(IStudyPlanRepository studyPlan, IUnitOfWork uow)
    {
        _studyPlan = studyPlan;
        _uow = uow;
    }

    public Task<byte?> GetHoursAsync(Guid schoolYearId, Guid gradeId, Guid subjectId, CancellationToken ct = default)
        => _studyPlan.GetHoursAsync(schoolYearId, gradeId, subjectId, ct);

    public Task<IReadOnlyList<StudyPlanEntry>> ListByGradeAsync(Guid schoolYearId, Guid gradeId, CancellationToken ct = default)
        => _studyPlan.ListByGradeAsync(schoolYearId, gradeId, ct);

    public async Task UpsertAsync(Guid schoolYearId, Guid gradeId, Guid subjectId, byte weeklyHours, CancellationToken ct = default)
    {
        var entry = new StudyPlanEntry(schoolYearId, gradeId, subjectId, weeklyHours);
        await _studyPlan.UpsertAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
