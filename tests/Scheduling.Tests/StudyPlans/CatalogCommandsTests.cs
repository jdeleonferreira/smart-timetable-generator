using ErrorOr;
using SmartTimetableGenerator.Application.UseCases.Areas.Commands.AddSubject;
using SmartTimetableGenerator.Application.UseCases.Areas.Commands.CreateArea;
using SmartTimetableGenerator.Application.UseCases.Areas.Commands.UpdateArea;
using SmartTimetableGenerator.Application.UseCases.Areas.Commands.UpdateSubject;
using SmartTimetableGenerator.Application.UseCases.Areas.Queries.GetAreas;
using SmartTimetableGenerator.Application.UseCases.Grades.Commands.CreateGrade;
using SmartTimetableGenerator.Application.UseCases.Grades.Commands.UpdateGrade;
using SmartTimetableGenerator.Application.UseCases.Grades.Queries.GetGrades;
using SmartTimetableGenerator.Domain.Areas;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Scheduling.Tests.Common;

namespace SmartTimetableGenerator.Scheduling.Tests.StudyPlans;

/// <summary>
/// Catálogo del plan de estudios por MediatR (con validadores y EF Core): grados, áreas y asignaturas.
/// </summary>
public sealed class CatalogCommandsTests : IDisposable
{
    private readonly SchedulingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private async Task<TestSchool> SeedAsync()
    {
        var school = new TestSchool();
        await _host.SeedAsync(school.Seed);
        return school;
    }

    #region Grados

    [Fact]
    public async Task GetGrades_ShouldReturnTheCatalogInPresentationOrder()
    {
        var school = await SeedAsync();
        await _host.SendAsync(new CreateGradeCommand("Quinto", "5º", EducationLevel.Primary, 5));

        var grades = await _host.SendAsync(new GetGradesQuery());

        grades.Select(g => g.ShortName).Should().Equal("5º", "6º", "7º");
        grades.Single(g => g.Id == school.Sixth.Id.Value).Should().BeEquivalentTo(
            new GradeDto(school.Sixth.Id.Value, "Sexto", "6º", EducationLevel.LowerSecondary, 6));
    }

    [Fact]
    public async Task CreateGrade_WithAnExistingNameInAnotherCase_ShouldReturnDuplicateName()
    {
        await SeedAsync();

        var result = await _host.SendAsync(new CreateGradeCommand("  SEXTO ", "6", EducationLevel.LowerSecondary, 60));

        result.FirstError.Should().Be(GradeErrors.DuplicateName);
        (await _host.SendAsync(new GetGradesQuery())).Should().HaveCount(2);
    }

    [Theory]
    [InlineData("", "5º", 5)]
    [InlineData("Quinto", "", 5)]
    [InlineData("Quinto", "5º", -1)]
    [InlineData("Quinto", "DEMASIADOLARGO", 5)]
    public async Task CreateGrade_WithInvalidData_ShouldReturnValidationErrorsAndSaveNothing(string name, string shortName, int order)
    {
        await SeedAsync();

        var result = await _host.SendAsync(new CreateGradeCommand(name, shortName, EducationLevel.Primary, order));

        result.IsError.Should().BeTrue();
        result.Errors.Should().OnlyContain(e => e.Type == ErrorType.Validation);
        (await _host.SendAsync(new GetGradesQuery())).Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateGrade_ShouldChangeItAndRejectTheNameOfAnotherGrade()
    {
        var school = await SeedAsync();

        var ok = await _host.SendAsync(new UpdateGradeCommand("Grado sexto", "6°", EducationLevel.LowerSecondary, 16) { GradeId = school.Sixth.Id.Value });
        var duplicate = await _host.SendAsync(new UpdateGradeCommand("séptimo", "6°", EducationLevel.LowerSecondary, 16) { GradeId = school.Sixth.Id.Value });
        var sameName = await _host.SendAsync(new UpdateGradeCommand("Grado sexto", "6", EducationLevel.LowerSecondary, 16) { GradeId = school.Sixth.Id.Value });
        var missing = await _host.SendAsync(new UpdateGradeCommand("X", "X", EducationLevel.Primary, 1) { GradeId = Guid.NewGuid() });

        ok.IsError.Should().BeFalse();
        duplicate.FirstError.Should().Be(GradeErrors.DuplicateName);
        sameName.IsError.Should().BeFalse("un grado puede conservar su propio nombre");
        missing.FirstError.Should().Be(GradeErrors.NotFound);

        var grades = await _host.SendAsync(new GetGradesQuery());
        grades.Select(g => g.ShortName).Should().Equal("7º", "6");
        grades.Last().Name.Should().Be("Grado sexto");
    }

    #endregion

    #region Áreas y asignaturas

    [Fact]
    public async Task CreateArea_WithoutOrder_ShouldGoLast_AndRejectDuplicateNames()
    {
        await SeedAsync();

        var created = await _host.SendAsync(new CreateAreaCommand("Educación Artística"));
        var duplicate = await _host.SendAsync(new CreateAreaCommand("educación artística"));

        created.IsError.Should().BeFalse();
        duplicate.FirstError.Should().Be(AreaErrors.DuplicateName);

        var areas = await _host.SendAsync(new GetAreasQuery());
        areas.Should().HaveCount(7);
        areas[^1].Should().BeEquivalentTo(new AreaDto(created.Value, "Educación Artística", 6, []));
    }

    [Fact]
    public async Task UpdateArea_ShouldReorderAndRejectTheNameOfAnotherArea()
    {
        var school = await SeedAsync();

        var moved = await _host.SendAsync(new UpdateAreaCommand("Matemáticas", 99) { AreaId = school.Math.Id.Value });
        var duplicate = await _host.SendAsync(new UpdateAreaCommand("HUMANIDADES", 0) { AreaId = school.Math.Id.Value });
        var missing = await _host.SendAsync(new UpdateAreaCommand("Otra", 0) { AreaId = Guid.NewGuid() });

        moved.IsError.Should().BeFalse();
        duplicate.FirstError.Should().Be(AreaErrors.DuplicateName);
        missing.FirstError.Should().Be(AreaErrors.NotFound);
        (await _host.SendAsync(new GetAreasQuery()))[^1].Name.Should().Be("Matemáticas");
    }

    [Fact]
    public async Task AddSubject_ShouldAddItWithAnUppercaseCode_AndRejectDuplicatesOnlyWithinTheArea()
    {
        var school = await SeedAsync();

        var geometry = await _host.SendAsync(new AddSubjectCommand("Geometría", "geo") { AreaId = school.Math.Id.Value });
        var duplicate = await _host.SendAsync(new AddSubjectCommand("GEOMETRÍA") { AreaId = school.Math.Id.Value });
        var otherArea = await _host.SendAsync(new AddSubjectCommand("Geometría") { AreaId = school.Technology.Id.Value });
        var missingArea = await _host.SendAsync(new AddSubjectCommand("Estadística") { AreaId = Guid.NewGuid() });

        geometry.IsError.Should().BeFalse();
        duplicate.FirstError.Should().Be(AreaErrors.DuplicateSubjectName);
        otherArea.IsError.Should().BeFalse("el nombre solo debe ser único dentro del área");
        missingArea.FirstError.Should().Be(AreaErrors.NotFound);

        var math = (await _host.SendAsync(new GetAreasQuery())).Single(a => a.Id == school.Math.Id.Value);
        math.Subjects.Select(s => s.Name).Should().Equal("Aritmética", "Geometría");
        math.Subjects[1].Should().BeEquivalentTo(new SubjectDto(geometry.Value, "Geometría", "GEO", 1, true));
    }

    [Fact]
    public async Task UpdateSubject_ShouldDeactivateIt_AndHideItWhenOnlyActiveSubjectsAreRequested()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new UpdateSubjectCommand("Competencia ciudadana", "cc", 0, IsActive: false)
        {
            AreaId = school.Ethics.Id.Value,
            SubjectId = school.Citizenship.Id.Value
        });

        result.IsError.Should().BeFalse();
        var all = await _host.SendAsync(new GetAreasQuery());
        all.Single(a => a.Id == school.Ethics.Id.Value).Subjects.Single()
            .Should().BeEquivalentTo(new SubjectDto(school.Citizenship.Id.Value, "Competencia ciudadana", "CC", 0, false));

        var active = await _host.SendAsync(new GetAreasQuery(IncludeInactiveSubjects: false));
        active.Single(a => a.Id == school.Ethics.Id.Value).Subjects.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateSubject_OfAnotherArea_ShouldReturnSubjectNotFound()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new UpdateSubjectCommand("Aritmética", null, 0)
        {
            AreaId = school.Humanities.Id.Value,
            SubjectId = school.Arithmetic.Id.Value
        });

        result.FirstError.Should().Be(AreaErrors.SubjectNotFound);
    }

    #endregion
}
