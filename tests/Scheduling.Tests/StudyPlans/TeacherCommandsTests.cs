using ErrorOr;
using Microsoft.EntityFrameworkCore;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Catalog.Queries;
using SmartTimetableGenerator.Application.UseCases.Teachers.Commands.CreateTeacher;
using SmartTimetableGenerator.Application.UseCases.Teachers.Commands.SetTeachingAssignment;
using SmartTimetableGenerator.Application.UseCases.Teachers.Commands.UpdateTeacher;
using SmartTimetableGenerator.Application.UseCases.Teachers.Queries.GetTeachingAssignments;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.TeachingAssignments;
using SmartTimetableGenerator.Domain.Timetables;
using SmartTimetableGenerator.Scheduling.Tests.Common;

namespace SmartTimetableGenerator.Scheduling.Tests.StudyPlans;

/// <summary>
/// Gestión de docentes y asignación académica por MediatR (con validadores y EF Core).
/// </summary>
public sealed class TeacherCommandsTests : IDisposable
{
    private readonly SchedulingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private async Task<TestSchool> SeedAsync()
    {
        var school = new TestSchool();
        await _host.SeedAsync(school.Seed);
        return school;
    }

    [Fact]
    public async Task CreateTeacher_ShouldRegisterTheTeacherWithAreasAndCampuses()
    {
        var school = await SeedAsync();

        var created = await _host.SendAsync(new CreateTeacherCommand(
            "Gloria", "Pérez", 20, [school.Math.Id.Value], [school.Campus.Id.Value], "Gloria@Colegio.test", "3001234567", 6));

        created.IsError.Should().BeFalse();
        var teachers = await _host.SendAsync(new GetTeachersQuery());
        var teacher = teachers.Single(t => t.Id == created.Value);
        teacher.FullName.Should().Be("Gloria Pérez");
        teacher.Email.Should().Be("gloria@colegio.test");
        teacher.MaxWeeklyHours.Should().Be(20);
        teacher.MaxDailyHours.Should().Be(6);
        teacher.AreaIds.Should().Equal(school.Math.Id.Value);
        teacher.CampusIds.Should().Equal(school.Campus.Id.Value);
        teacher.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateTeacher_WithAnExistingEmail_ShouldReturnDuplicateEmail()
    {
        var school = await SeedAsync();
        var email = school.MathTeacher.Email!.ToUpperInvariant();

        var result = await _host.SendAsync(new CreateTeacherCommand("Otra", "Persona", 20, [], [], email));

        result.FirstError.Should().Be(TeacherErrors.DuplicateEmail);
    }

    [Fact]
    public async Task CreateTeacher_WithAnUnknownArea_ShouldReturnNotFound()
    {
        await SeedAsync();

        var result = await _host.SendAsync(new CreateTeacherCommand("Otra", "Persona", 20, [Guid.NewGuid()], []));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task CreateTeacher_AsCoordinator_ShouldBeForbidden()
    {
        var school = await SeedAsync();
        _host.SignInAs(Roles.Coordinator, school.Campus.Id);

        var result = await _host.SendAsync(new CreateTeacherCommand("Otra", "Persona", 20, [], []));

        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateTeacher_ShouldChangeTheDataAndDeactivate()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new UpdateTeacherCommand(
            "Ana", "Nueva", 18, [school.Math.Id.Value, school.Science.Id.Value], [school.Campus.Id.Value], null, null, null, null, IsActive: false)
        { TeacherId = school.MathTeacher.Id.Value });

        result.IsError.Should().BeFalse();
        var teacher = (await _host.SendAsync(new GetTeachersQuery())).Single(t => t.Id == school.MathTeacher.Id.Value);
        teacher.LastName.Should().Be("Nueva");
        teacher.MaxWeeklyHours.Should().Be(18);
        teacher.AreaIds.Should().HaveCount(2);
        teacher.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SetTeachingAssignment_ShouldFixTheTeacher_AndUpdateTheLessonsOfDraftTimetables()
    {
        var school = await SeedAsync();
        await _host.WithDbAsync(async db =>
        {
            var timetable = await db.Timetables.Include(t => t.Lessons).FirstAsync();
            timetable.AddLesson(new LessonPlacement(school.C6A.Id, school.Arithmetic.Id, null, null, school.Morning.Id, DayOfWeek.Monday, 1))
                .IsError.Should().BeFalse();
            await db.SaveChangesAsync(CancellationToken.None);
            return 0;
        });

        var result = await _host.SendAsync(new SetTeachingAssignmentCommand(school.C6A.Id.Value, school.Arithmetic.Id.Value, school.MathTeacher.Id.Value));

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(new SetTeachingAssignmentResult(1, 0));
        var assignments = await _host.SendAsync(new GetTeachingAssignmentsQuery(school.Year.Id.Value, school.Campus.Id.Value));
        assignments.Should().ContainSingle(a => a.CourseId == school.C6A.Id.Value && a.SubjectId == school.Arithmetic.Id.Value)
            .Which.Should().Be(new TeachingAssignmentDto(school.C6A.Id.Value, school.Arithmetic.Id.Value, school.MathTeacher.Id.Value, true));
        var lesson = await _host.WithDbAsync(db => db.Timetables.Include(t => t.Lessons).SelectMany(t => t.Lessons).SingleAsync());
        lesson.TeacherId.Should().Be(school.MathTeacher.Id);
    }

    [Fact]
    public async Task SetTeachingAssignment_WithNullTeacher_ShouldReleaseTheAssignment()
    {
        var school = await SeedAsync();
        await _host.SendAsync(new SetTeachingAssignmentCommand(school.C6A.Id.Value, school.Arithmetic.Id.Value, school.MathTeacher.Id.Value));

        var result = await _host.SendAsync(new SetTeachingAssignmentCommand(school.C6A.Id.Value, school.Arithmetic.Id.Value, null));

        result.IsError.Should().BeFalse();
        var assignment = (await _host.SendAsync(new GetTeachingAssignmentsQuery(school.Year.Id.Value, school.Campus.Id.Value))).Single();
        assignment.TeacherId.Should().BeNull();
        assignment.IsManual.Should().BeFalse();
    }

    [Fact]
    public async Task SetTeachingAssignment_WithATeacherOfAnotherArea_ShouldBeRejected()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new SetTeachingAssignmentCommand(school.C6A.Id.Value, school.Arithmetic.Id.Value, school.SportsTeacher.Id.Value));

        result.FirstError.Should().Be(TeachingAssignmentErrors.TeacherCannotTeachArea);
    }

    [Fact]
    public async Task SetTeachingAssignment_AsCoordinatorOfAnotherCampus_ShouldBeForbidden()
    {
        var school = await SeedAsync();
        _host.SignInAs(Roles.Coordinator, school.OtherCampus.Id);

        var result = await _host.SendAsync(new SetTeachingAssignmentCommand(school.C6A.Id.Value, school.Arithmetic.Id.Value, school.MathTeacher.Id.Value));

        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
    }
}
