using ErrorOr;
using Microsoft.EntityFrameworkCore;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Areas.Commands.AddSubject;
using SmartTimetableGenerator.Application.UseCases.Areas.Commands.CreateArea;
using SmartTimetableGenerator.Application.UseCases.Grades.Commands.CreateGrade;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.AddStudyPlanItem;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.ApproveStudyPlan;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.CreateStudyPlan;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.RemoveStudyPlanItem;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.SetStudyPlanItemDistribution;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.SetStudyPlanItemPeriodHours;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.UpdateStudyPlan;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Commands.UpdateStudyPlanItem;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Queries.GetStudyPlan;
using SmartTimetableGenerator.Application.UseCases.Timetables.Commands.CreateTimetable;
using SmartTimetableGenerator.Application.UseCases.Timetables.Commands.GenerateTimetable;
using SmartTimetableGenerator.Application.UseCases.Timetables.Commands.PublishTimetable;
using SmartTimetableGenerator.Application.UseCases.Timetables.Queries.GetTimetableLessons;
using SmartTimetableGenerator.Application.UseCases.Timetables.Queries.GetTimetables;
using SmartTimetableGenerator.Application.UseCases.Users.Queries.GetUsers;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.Timetables;
using SmartTimetableGenerator.Scheduling.Tests.Common;

namespace SmartTimetableGenerator.Scheduling.Tests.Security;

/// <summary>
/// Permisos por rol y por sede, aplicados en la capa de aplicación (atributo Authorize + comprobaciones de sede):
/// el administrador gestiona todo; el coordinador, el plan y los horarios de su sede; el docente solo consulta
/// y solo ve horarios publicados. Sin sesión no se ejecuta ningún comando.
/// </summary>
public sealed class PermissionTests : IDisposable
{
    private readonly SchedulingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private async Task<(TestSchool School, StudyPlan OtherPlan, Timetable OtherTimetable)> SeedAsync()
    {
        var school = new TestSchool();
        var otherPlan = StudyPlan.Create(school.Year.Id, school.OtherCampus.Id, "Plan Sede Norte");
        otherPlan.AddItem(school.Sixth.Id, school.Arithmetic.Id, DeliveryMode.Regular, 4).IsError.Should().BeFalse();
        var otherTimetable = Timetable.Create(school.Year.Id, school.OtherCampus.Id, school.Period1.Id, otherPlan.Id, "Horario Sede Norte");
        otherTimetable.AddLesson(new LessonPlacement(school.C6A.Id, school.Arithmetic.Id, null, null, school.OtherMorning.Id, DayOfWeek.Monday, 1))
            .IsError.Should().BeFalse();
        await _host.SeedAsync(db =>
        {
            school.Seed(db);
            db.StudyPlans.Add(otherPlan);
            db.Timetables.Add(otherTimetable);
        });
        return (school, otherPlan, otherTimetable);
    }

    /// <summary>Todos los comandos que modifican el plan de estudios de <paramref name="plan"/>.</summary>
    private async Task<List<Error>> ChangePlanAsync(StudyPlan plan, TestSchool school)
    {
        var item = plan.Items[0].Id.Value;
        var id = plan.Id.Value;
        return
        [
            (await _host.SendAsync(new UpdateStudyPlanCommand("Nuevo nombre", null) { StudyPlanId = id })).FirstOrSuccess(),
            (await _host.SendAsync(new AddStudyPlanItemCommand(school.Seventh.Id.Value, school.Research.Id.Value, DeliveryMode.Regular, 1) { StudyPlanId = id })).FirstOrSuccess(),
            (await _host.SendAsync(new UpdateStudyPlanItemCommand(DeliveryMode.Regular, 3) { StudyPlanId = id, ItemId = item })).FirstOrSuccess(),
            (await _host.SendAsync(new SetStudyPlanItemDistributionCommand(2, 2, null) { StudyPlanId = id, ItemId = item })).FirstOrSuccess(),
            (await _host.SendAsync(new SetStudyPlanItemPeriodHoursCommand(2) { StudyPlanId = id, ItemId = item, AcademicPeriodId = school.Period2.Id.Value })).FirstOrSuccess(),
            (await _host.SendAsync(new ApproveStudyPlanCommand(id))).FirstOrSuccess(),
            (await _host.SendAsync(new ReopenStudyPlanCommand(id))).FirstOrSuccess(),
            (await _host.SendAsync(new RemoveStudyPlanItemCommand(id, item))).FirstOrSuccess(),
        ];
    }

    private async Task<List<Error>> ChangeCatalogAsync(TestSchool school) =>
    [
        (await _host.SendAsync(new CreateAreaCommand("Artística"))).FirstOrSuccess(),
        (await _host.SendAsync(new AddSubjectCommand("Geometría") { AreaId = school.Math.Id.Value })).FirstOrSuccess(),
        (await _host.SendAsync(new CreateGradeCommand("Octavo", "8º", EducationLevel.LowerSecondary, 8))).FirstOrSuccess(),
    ];

    [Fact]
    public async Task WithoutSession_EveryCommandShouldReturnUnauthenticated_AndChangeNothing()
    {
        var (school, _, _) = await SeedAsync();
        _host.SignInAs(null);

        var errors = new List<Error>();
        errors.AddRange(await ChangePlanAsync(school.Plan, school));
        errors.AddRange(await ChangeCatalogAsync(school));
        errors.Add((await _host.SendAsync(new CreateTimetableCommand(school.Year.Id.Value, school.Campus.Id.Value, school.Period2.Id.Value, null))).FirstOrSuccess());
        errors.Add((await _host.SendAsync(new GenerateTimetableCommand(30) { TimetableId = school.Timetable.Id.Value })).FirstOrSuccess());
        errors.Add((await _host.SendAsync(new GetUsersQuery())).FirstOrSuccess());

        errors.Should().HaveCount(14).And.AllBeEquivalentTo(SecurityErrors.Unauthenticated);
        await ShouldBeUnchangedAsync(school);
    }

    [Fact]
    public async Task Teacher_ShouldNotChangeAnything()
    {
        var (school, _, _) = await SeedAsync();
        _host.SignInAs(Roles.Teacher, teacherId: school.MathTeacher.Id);

        var errors = new List<Error>();
        errors.AddRange(await ChangePlanAsync(school.Plan, school));
        errors.AddRange(await ChangeCatalogAsync(school));
        errors.Add((await _host.SendAsync(new CreateStudyPlanCommand(school.Year.Id.Value, school.Campus.Id.Value))).FirstOrSuccess());
        errors.Add((await _host.SendAsync(new CreateTimetableCommand(school.Year.Id.Value, school.Campus.Id.Value, school.Period2.Id.Value, null))).FirstOrSuccess());
        errors.Add((await _host.SendAsync(new GenerateTimetableCommand(30) { TimetableId = school.Timetable.Id.Value })).FirstOrSuccess());
        errors.Add((await _host.SendAsync(new PublishTimetableCommand(school.Timetable.Id.Value))).FirstOrSuccess());
        errors.Add((await _host.SendAsync(new GetUsersQuery())).FirstOrSuccess());

        errors.Should().HaveCount(16).And.AllBeEquivalentTo(SecurityErrors.Forbidden);
        await ShouldBeUnchangedAsync(school);
    }

    [Fact]
    public async Task Teacher_CanReadThePlan_ButOnlySeesPublishedTimetables()
    {
        var (school, _, otherTimetable) = await SeedAsync();
        var published = await _host.WithDbAsync(async db =>
        {
            var timetable = await db.Timetables.Include(t => t.Lessons).FirstAsync(t => t.Id == otherTimetable.Id);
            return timetable.Publish(DateTimeOffset.UtcNow);
        });
        published.IsError.Should().BeFalse();
        _host.SignInAs(Roles.Teacher, teacherId: school.MathTeacher.Id);

        var plan = await _host.SendAsync(new GetStudyPlanQuery(school.Plan.Id.Value));
        var timetables = await _host.SendAsync(new GetTimetablesQuery(null, null));
        var draftLessons = await _host.SendAsync(new GetTimetableLessonsQuery(school.Timetable.Id.Value, null, null, null));
        var publishedLessons = await _host.SendAsync(new GetTimetableLessonsQuery(otherTimetable.Id.Value, null, null, null));

        plan.IsError.Should().BeFalse();
        timetables.Should().ContainSingle().Which.Id.Should().Be(otherTimetable.Id.Value);
        draftLessons.FirstError.Should().Be(TimetableErrors.NotFound, "un borrador no existe para el docente");
        publishedLessons.IsError.Should().BeFalse();

        _host.SignInAs(Roles.Coordinator, school.Campus.Id);
        (await _host.SendAsync(new GetTimetablesQuery(null, null))).Should().HaveCount(2, "el coordinador ve también los borradores");
    }

    [Fact]
    public async Task Coordinator_ShouldManageThePlanOfItsCampus()
    {
        var (school, _, _) = await SeedAsync();
        _host.SignInAs(Roles.Coordinator, school.Campus.Id);

        var results = await ChangePlanAsync(school.Plan, school);

        results.Should().OnlyContain(e => e.Code == TestResults.SuccessCode, "todas las operaciones sobre su sede deben funcionar");
        var plan = await _host.SendAsync(new GetStudyPlanQuery(school.Plan.Id.Value));
        plan.Value.Name.Should().Be("Nuevo nombre");
    }

    [Fact]
    public async Task Coordinator_ShouldNotTouchAnotherCampus()
    {
        var (school, otherPlan, otherTimetable) = await SeedAsync();
        _host.SignInAs(Roles.Coordinator, school.Campus.Id);

        var errors = new List<Error>();
        errors.AddRange(await ChangePlanAsync(otherPlan, school));
        errors.Add((await _host.SendAsync(new CreateTimetableCommand(school.Year.Id.Value, school.OtherCampus.Id.Value, school.Period2.Id.Value, null))).FirstOrSuccess());
        errors.Add((await _host.SendAsync(new GenerateTimetableCommand(30) { TimetableId = otherTimetable.Id.Value })).FirstOrSuccess());
        errors.Add((await _host.SendAsync(new PublishTimetableCommand(otherTimetable.Id.Value))).FirstOrSuccess());

        errors.Should().HaveCount(11).And.AllBeEquivalentTo(SecurityErrors.CampusForbidden);

        var otherAfter = await _host.WithDbAsync(db => db.StudyPlans.Include(p => p.Items).FirstAsync(p => p.Id == otherPlan.Id));
        otherAfter.Name.Should().Be("Plan Sede Norte");
        otherAfter.Items.Should().ContainSingle().Which.WeeklyHours.Should().Be(4);
        (await _host.WithDbAsync(db => db.GenerationJobs.CountAsync())).Should().Be(0);
        (await _host.WithDbAsync(db => db.Timetables.CountAsync())).Should().Be(2);
    }

    [Fact]
    public async Task Coordinator_ShouldNotCreateAPlanForAnotherCampus_NorChangeTheInstitutionalCatalog()
    {
        var (school, _, _) = await SeedAsync();
        var nextYear = SmartTimetableGenerator.Domain.AcademicYears.AcademicYear.Create(2027, new DateOnly(2027, 1, 25), new DateOnly(2027, 11, 26));
        await _host.SeedAsync(db => db.AcademicYears.Add(nextYear));
        _host.SignInAs(Roles.Coordinator, school.Campus.Id);

        var otherCampus = await _host.SendAsync(new CreateStudyPlanCommand(nextYear.Id.Value, school.OtherCampus.Id.Value));
        var ownCampus = await _host.SendAsync(new CreateStudyPlanCommand(nextYear.Id.Value, school.Campus.Id.Value));
        var catalog = await ChangeCatalogAsync(school);

        otherCampus.FirstError.Should().Be(SecurityErrors.CampusForbidden);
        ownCampus.IsError.Should().BeFalse();
        catalog.Should().AllBeEquivalentTo(SecurityErrors.Forbidden);
    }

    [Fact]
    public async Task CoordinatorWithoutCampus_ShouldNotManageAnyCampus()
    {
        var (school, _, _) = await SeedAsync();
        _host.SignInAs(Roles.Coordinator);

        var errors = await ChangePlanAsync(school.Plan, school);

        errors.Should().AllBeEquivalentTo(SecurityErrors.CampusForbidden);
    }

    [Fact]
    public async Task Admin_ShouldManageEveryCampusAndTheCatalog()
    {
        var (school, otherPlan, otherTimetable) = await SeedAsync();
        _host.SignInAs(Roles.Admin);

        var plan = await ChangePlanAsync(otherPlan, school);
        var catalog = await ChangeCatalogAsync(school);
        var generate = await _host.SendAsync(new GenerateTimetableCommand(30) { TimetableId = otherTimetable.Id.Value });

        plan.Concat(catalog).Should().OnlyContain(e => e.Code == TestResults.SuccessCode);
        generate.IsError.Should().BeFalse();
    }

    private async Task ShouldBeUnchangedAsync(TestSchool school)
    {
        _host.SignInAs(Roles.Admin);
        var plan = await _host.SendAsync(new GetStudyPlanQuery(school.Plan.Id.Value));
        plan.Value.Name.Should().Be("Plan de prueba");
        plan.Value.Status.Should().Be(StudyPlanStatus.Draft);
        plan.Value.Areas.SelectMany(a => a.Subjects).Sum(s => s.Items.Count).Should().Be(12);
        plan.Value.Areas.Should().HaveCount(6);
        (await _host.WithDbAsync(db => db.Grades.CountAsync())).Should().Be(2);
        (await _host.WithDbAsync(db => db.GenerationJobs.CountAsync())).Should().Be(0);
        (await _host.WithDbAsync(db => db.Timetables.CountAsync())).Should().Be(2);
    }
}

internal static class ErrorOrTestExtensions
{
    /// <summary>
    /// Primer error, o un "error" de código <see cref="TestResults.SuccessCode"/> si la operación funcionó
    /// (para comparar listas de resultados de distintos tipos).
    /// </summary>
    public static Error FirstOrSuccess<T>(this ErrorOr<T> result) =>
        result.IsError ? result.FirstError : Error.Custom(0, TestResults.SuccessCode, "Correcto");
}

internal static class TestResults
{
    public const string SuccessCode = "Test.Success";
}
