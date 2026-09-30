using ErrorOr;
using Microsoft.EntityFrameworkCore;
using SmartTimetableGenerator.Application.UseCases.GenerationJobs.Queries.GetGenerationJob;
using SmartTimetableGenerator.Application.UseCases.Timetables.Commands.CreateTimetable;
using SmartTimetableGenerator.Application.UseCases.Timetables.Commands.GenerateTimetable;
using SmartTimetableGenerator.Application.UseCases.Timetables.Commands.PublishTimetable;
using SmartTimetableGenerator.Application.UseCases.Timetables.Queries.GetTimetableLessons;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.TimetableGeneration;
using SmartTimetableGenerator.Domain.Timetables;
using SmartTimetableGenerator.Scheduling.Tests.Common;

namespace SmartTimetableGenerator.Scheduling.Tests.Generation;

/// <summary>
/// Flujo de la API por MediatR (con validadores): crear, generar, consultar y publicar un horario.
/// </summary>
public sealed class TimetableCommandsTests : IDisposable
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
    public async Task CreateTimetable_ShouldCreateADraftLinkedToTheCampusPlan_WithADefaultName()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new CreateTimetableCommand(school.Year.Id.Value, school.Campus.Id.Value, school.Period2.Id.Value, null));

        result.IsError.Should().BeFalse();
        var created = await _host.WithDbAsync(db => db.Timetables.FirstAsync(t => t.Id == TimetableId.From(result.Value)));
        created.Status.Should().Be(TimetableStatus.Draft);
        created.StudyPlanId.Should().Be(school.Plan.Id);
        created.AcademicPeriodId.Should().Be(school.Period2.Id);
        created.Name.Should().Be("Horario Sede Principal - Periodo 2 2026");
    }

    [Fact]
    public async Task CreateTimetable_ForACampusWithoutPlan_ShouldReturnNotFound()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new CreateTimetableCommand(school.Year.Id.Value, school.OtherCampus.Id.Value, school.Period1.Id.Value, null));

        result.FirstError.Should().Be(StudyPlanErrors.NotFound);
    }

    [Fact]
    public async Task CreateTimetable_WithAPeriodOfAnotherYear_ShouldReturnPeriodNotFound()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new CreateTimetableCommand(school.Year.Id.Value, school.Campus.Id.Value, Guid.NewGuid(), null));

        result.FirstError.Should().Be(AcademicYearErrors.PeriodNotFound);
    }

    [Fact]
    public async Task GenerateTimetable_ShouldQueueAJob()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new GenerateTimetableCommand(30) { TimetableId = school.Timetable.Id.Value });

        result.IsError.Should().BeFalse();
        var job = await _host.SendAsync(new GetGenerationJobQuery(result.Value));
        job.Value.Status.Should().Be(nameof(GenerationJobStatus.Queued));
        job.Value.IsFinished.Should().BeFalse();
        job.Value.TimeLimitSeconds.Should().Be(30);
        job.Value.TimetableId.Should().Be(school.Timetable.Id.Value);
    }

    [Fact]
    public async Task GenerateTimetable_WhileAnotherJobIsPending_ShouldReturnAlreadyRunning()
    {
        var school = await SeedAsync();
        await _host.SendAsync(new GenerateTimetableCommand(30) { TimetableId = school.Timetable.Id.Value });

        var second = await _host.SendAsync(new GenerateTimetableCommand(30) { TimetableId = school.Timetable.Id.Value });

        second.FirstError.Should().Be(GenerationJobErrors.AlreadyRunning);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(1801)]
    public async Task GenerateTimetable_WithAnInvalidTimeLimit_ShouldReturnAValidationError(int seconds)
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new GenerateTimetableCommand(seconds) { TimetableId = school.Timetable.Id.Value });

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GenerateTimetable_ForAnUnknownTimetable_ShouldReturnNotFound()
    {
        await SeedAsync();

        var result = await _host.SendAsync(new GenerateTimetableCommand(30) { TimetableId = Guid.NewGuid() });

        result.FirstError.Should().Be(TimetableErrors.NotFound);
    }

    [Fact]
    public async Task PublishTimetable_WithoutLessons_ShouldFail()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new PublishTimetableCommand(school.Timetable.Id.Value));

        result.FirstError.Should().Be(TimetableErrors.Empty);
    }

    [Fact]
    public async Task FullFlow_Generate_Query_Publish_AndRejectFurtherGeneration()
    {
        var school = await SeedAsync();
        var queued = await _host.SendAsync(new GenerateTimetableCommand(30) { TimetableId = school.Timetable.Id.Value });
        await _host.RunJobAsync(GenerationJobId.From(queued.Value));

        var job = (await _host.SendAsync(new GetGenerationJobQuery(queued.Value))).Value;
        job.Status.Should().Be(nameof(GenerationJobStatus.Succeeded), job.Message);
        job.IsFinished.Should().BeTrue();
        job.PlacedLessons.Should().Be(4 * TestSchool.BaseWeeklyHours);
        job.UnplacedLessons.Should().Be(0);

        // Vista por curso: solo ese curso, ordenada por día y franja, con horas del timbre y nombres
        var byCourse = (await _host.SendAsync(new GetTimetableLessonsQuery(school.Timetable.Id.Value, school.C6A.Id.Value, null, null))).Value;
        byCourse.Should().HaveCount(TestSchool.BaseWeeklyHours);
        byCourse.Should().OnlyContain(l => l.CourseId == school.C6A.Id.Value && l.CourseName == "6ºA");
        byCourse.Should().OnlyContain(l => l.Start != null && l.End != null && l.Start < l.End && l.TeacherName != null && l.SpaceName == "Salón 101");
        byCourse.Select(l => ((int)l.Day * 10) + l.PeriodNumber).Should().BeInAscendingOrder();
        byCourse.Where(l => l.PeriodNumber == 1).Should().OnlyContain(l => l.Start == new TimeOnly(7, 0));
        byCourse.Where(l => l.PeriodNumber == 4).Should().OnlyContain(l => l.Start == new TimeOnly(10, 0));

        // Vista por docente y vista institucional de un día
        var byTeacher = (await _host.SendAsync(new GetTimetableLessonsQuery(school.Timetable.Id.Value, null, school.MathTeacher.Id.Value, null))).Value;
        byTeacher.Should().HaveCount(20);
        byTeacher.Should().OnlyContain(l => l.SubjectName == "Aritmética" && l.TeacherName == "Ana Matemáticas");
        var wednesday = (await _host.SendAsync(new GetTimetableLessonsQuery(school.Timetable.Id.Value, null, null, DayOfWeek.Wednesday))).Value;
        wednesday.Should().OnlyContain(l => l.Day == DayOfWeek.Wednesday);
        wednesday.Select(l => l.CourseId).Distinct().Should().HaveCount(4);

        // Publicar y ya no se puede generar
        (await _host.SendAsync(new PublishTimetableCommand(school.Timetable.Id.Value))).IsError.Should().BeFalse();
        var again = await _host.SendAsync(new GenerateTimetableCommand(30) { TimetableId = school.Timetable.Id.Value });
        again.FirstError.Should().Be(TimetableErrors.NotEditable);
    }

    [Fact]
    public async Task GetTimetableLessons_ForAnUnknownTimetable_ShouldReturnNotFound()
    {
        await SeedAsync();

        var result = await _host.SendAsync(new GetTimetableLessonsQuery(Guid.NewGuid(), null, null, null));

        result.FirstError.Should().Be(TimetableErrors.NotFound);
    }
}
