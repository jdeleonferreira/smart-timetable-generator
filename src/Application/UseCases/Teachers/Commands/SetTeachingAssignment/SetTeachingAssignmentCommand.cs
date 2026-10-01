using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Courses;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.TeachingAssignments;
using SmartTimetableGenerator.Domain.Timetables;

namespace SmartTimetableGenerator.Application.UseCases.Teachers.Commands.SetTeachingAssignment;

/// <summary>
/// Fija (o, con <c>TeacherId</c> nulo, libera) el docente de una asignatura en un curso para todo el año.
/// Las clases ya generadas en horarios en borrador se actualizan; las que el docente no puede dictar por cruce
/// de horario se dejan como estaban y se cuentan en <see cref="SetTeachingAssignmentResult.ConflictingLessons"/>.
/// </summary>
[Authorize(Roles = Roles.Managers)]
public sealed record SetTeachingAssignmentCommand(Guid CourseId, Guid SubjectId, Guid? TeacherId) : IRequest<ErrorOr<SetTeachingAssignmentResult>>;

public sealed record SetTeachingAssignmentResult(int UpdatedLessons, int ConflictingLessons);

internal sealed class SetTeachingAssignmentCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<SetTeachingAssignmentCommand, ErrorOr<SetTeachingAssignmentResult>>
{
    public async Task<ErrorOr<SetTeachingAssignmentResult>> Handle(SetTeachingAssignmentCommand request, CancellationToken cancellationToken)
    {
        var courseId = CourseId.From(request.CourseId);
        var subjectId = SubjectId.From(request.SubjectId);

        var course = await dbContext.Courses.FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
        if (course is null)
            return CourseErrors.NotFound;

        if (currentUser.EnsureCanManageCampus(course.CampusId) is { } forbidden)
            return forbidden;

        var area = await dbContext.Areas
            .WithSpecification(AreaSpec.BySubject(subjectId))
            .FirstOrDefaultAsync(cancellationToken);
        if (area is null)
            return AreaErrors.SubjectNotFound;

        TeacherId? teacherId = null;
        if (request.TeacherId is { } id)
        {
            var teacher = await dbContext.Teachers
                .WithSpecification(TeacherSpec.ById(TeacherId.From(id)))
                .FirstOrDefaultAsync(cancellationToken);
            if (teacher is null)
                return TeacherErrors.NotFound;

            if (!teacher.CanTeach(area.Id))
                return TeachingAssignmentErrors.TeacherCannotTeachArea;

            if (!teacher.WorksAt(course.CampusId))
                return TeachingAssignmentErrors.TeacherNotInCampus;

            teacherId = teacher.Id;
        }

        var assignment = await dbContext.TeachingAssignments.FirstOrDefaultAsync(
            a => a.AcademicYearId == course.AcademicYearId && a.CourseId == courseId &&
                 a.SubjectId == subjectId && a.AcademicPeriodId == null,
            cancellationToken);

        if (assignment is null)
        {
            assignment = TeachingAssignment.Create(course.AcademicYearId, courseId, subjectId);
            dbContext.TeachingAssignments.Add(assignment);
        }

        if (teacherId is { } tid)
            assignment.AssignManually(tid);
        else
            assignment.Release();

        // Actualiza las clases ya generadas en los borradores de la sede
        var timetables = await dbContext.Timetables
            .Include(t => t.Lessons)
            .Where(t => t.AcademicYearId == course.AcademicYearId && t.CampusId == course.CampusId && t.Status == TimetableStatus.Draft)
            .ToListAsync(cancellationToken);

        var updated = 0;
        var conflicting = 0;
        foreach (var timetable in timetables)
        {
            var lessons = timetable.Lessons
                .Where(l => l.CourseId == courseId && l.SubjectId == subjectId && l.TeacherId != teacherId)
                .ToList();

            foreach (var lesson in lessons)
            {
                if (timetable.ChangeLessonTeacher(lesson.Id, teacherId).IsError)
                    conflicting++;
                else
                    updated++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new SetTeachingAssignmentResult(updated, conflicting);
    }
}

internal sealed class SetTeachingAssignmentCommandValidator : AbstractValidator<SetTeachingAssignmentCommand>
{
    public SetTeachingAssignmentCommandValidator()
    {
        RuleFor(v => v.CourseId).NotEmpty();
        RuleFor(v => v.SubjectId).NotEmpty();
    }
}
