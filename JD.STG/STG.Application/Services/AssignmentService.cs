using STG.Application.Abstractions.Persistence;
using STG.Domain.Entities;

namespace STG.Application.Services;

public sealed class AssignmentService
{
    private readonly IAssignmentRepository _assignments;
    private readonly IStudyPlanRepository _studyPlan;
    private readonly ITeacherRepository _teachers;
    private readonly IGroupRepository _groups;
    private readonly IUnitOfWork _uow;

    public AssignmentService(
        IAssignmentRepository assignments,
        IStudyPlanRepository studyPlan,
        ITeacherRepository teachers,
        IGroupRepository groups,
        IUnitOfWork uow)
    {
        _assignments = assignments;
        _studyPlan = studyPlan;
        _teachers = teachers;
        _groups = groups;
        _uow = uow;
    }

    // Create
    public async Task<Guid> CreateAsync(Guid schoolYearId, Guid groupId, Guid subjectId, Guid teacherId, byte weeklyHours, CancellationToken ct = default)
    {
        // 1) Validaciones mínimas
        var ihPlan = await _studyPlan.GetHoursAsync(schoolYearId, gradeId: await ResolveGradeId(groupId, ct), subjectId, ct);
        if (ihPlan is null || weeklyHours > ihPlan.Value)
            throw new InvalidOperationException("WeeklyHours exceeds study plan IH.");

        // 2) Evitar duplicados grupo+materia
        if (await _assignments.ExistsForGroupSubjectAsync(schoolYearId, groupId, subjectId, ct))
            throw new InvalidOperationException("Assignment already exists for Group+Subject.");

        // 3) Crear
        var entity = new Assignment(schoolYearId, groupId, subjectId, teacherId, weeklyHours);
        await _assignments.AddAsync(entity, ct);

        await _uow.SaveChangesAsync(ct);
        return entity.Id;
    }

    // Update teacher / IH
    public async Task UpdateTeacherAsync(Guid assignmentId, Guid newTeacherId, CancellationToken ct = default)
    {
        var a = await _assignments.GetByIdAsync(assignmentId, ct) ?? throw new KeyNotFoundException("Assignment not found.");
        a.ChangeTeacher(newTeacherId);
        _assignments.Update(a);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task UpdateWeeklyHoursAsync(Guid assignmentId, byte ih, CancellationToken ct = default)
    {
        var a = await _assignments.GetByIdAsync(assignmentId, ct) ?? throw new KeyNotFoundException("Assignment not found.");
        // Validar contra plan
        var gradeId = await ResolveGradeId(a.GroupId, ct);
        var ihPlan = await _studyPlan.GetHoursAsync(a.SchoolYearId, gradeId, a.SubjectId, ct) ?? 0;
        if (ih == 0 || ih > ihPlan) throw new InvalidOperationException("IH exceeds plan.");
        a.ChangeWeeklyHours(ih);
        _assignments.Update(a);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<Guid> ResolveGradeId(Guid groupId, CancellationToken ct)
    {
        var g = await _groups.GetByIdAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found.");
        return g.GradeId;
    }
}