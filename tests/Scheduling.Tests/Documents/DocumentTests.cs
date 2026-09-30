using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using SmartTimetableGenerator.Application.Common.Documents;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Documents.Queries.GetStudyPlanDocument;
using SmartTimetableGenerator.Application.UseCases.Documents.Queries.GetTimetableDocument;
using SmartTimetableGenerator.Domain.Institutions;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.TimetableGeneration;
using SmartTimetableGenerator.Domain.Timetables;
using SmartTimetableGenerator.Domain.TrainingProjects;
using SmartTimetableGenerator.Scheduling.Tests.Common;
using UglyToad.PdfPig;

namespace SmartTimetableGenerator.Scheduling.Tests.Documents;

/// <summary>
/// Documentos del plan de estudios y de los horarios. Cada archivo se abre de nuevo (PdfPig para el PDF,
/// Open XML SDK para el Word) y se revisa su contenido; el Word además debe pasar la validación del esquema.
/// </summary>
public sealed class DocumentTests : IDisposable
{
    private const string InstitutionName = "Institución Educativa La Esperanza";

    private readonly SchedulingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private async Task<TestSchool> SeedAsync(Action<TestSchool>? adjust = null, bool withProjects = false)
    {
        var school = new TestSchool();
        adjust?.Invoke(school);
        await _host.SeedAsync(db =>
        {
            school.Seed(db);
            db.Institutions.Add(Institution.Create(InstitutionName));
            if (withProjects)
            {
                var festival = TrainingProject.Create(school.Year.Id, "Festival de porras", ProjectCategory.Institutional);
                festival.SetResponsible(school.SportsTeacher.Id, null);
                var democracy = TrainingProject.Create(school.Year.Id, "Educación para la democracia", ProjectCategory.Mandatory);
                democracy.SetResponsible(null, "Consejo estudiantil");
                var otherCampus = TrainingProject.Create(school.Year.Id, "Huerta escolar", ProjectCategory.Institutional, school.OtherCampus.Id);
                db.TrainingProjects.AddRange(festival, democracy, otherCampus);
            }
        });
        return school;
    }

    private static List<string> PdfPages(DocumentFile file)
    {
        file.ContentType.Should().Be("application/pdf");
        file.Content.Take(5).Should().Equal("%PDF-"u8.ToArray());
        using var pdf = PdfDocument.Open(file.Content);
        return pdf.GetPages().Select(p => string.Join(" ", p.GetWords().Select(w => w.Text))).ToList();
    }

    private static WordprocessingDocument OpenWord(DocumentFile file)
    {
        file.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        file.FileName.Should().EndWith(".docx");
        var word = WordprocessingDocument.Open(new MemoryStream(file.Content), false);
        var errors = new OpenXmlValidator().Validate(word).Select(e => $"{e.Path?.XPath}: {e.Description}").ToList();
        errors.Should().BeEmpty("el documento debe abrir en Word sin reparaciones");
        return word;
    }

    private static List<List<string>> WordTable(Table table) =>
        table.Elements<TableRow>().Select(r => r.Elements<TableCell>().Select(c => c.InnerText).ToList()).ToList();

    #region Plan de estudios

    [Fact]
    public async Task StudyPlanPdf_ShouldHaveTheInstitutionalHeaderTheGradesAndTheTotals()
    {
        var school = await SeedAsync();

        var result = await _host.SendAsync(new GetStudyPlanDocumentQuery(school.Plan.Id.Value, ReportFormat.Pdf));

        result.IsError.Should().BeFalse();
        result.Value.FileName.Should().Be("Plan de prueba.pdf");
        var pages = PdfPages(result.Value);
        pages.Should().ContainSingle("sin proyectos de formación el plan cabe en una página");
        var text = pages[0];
        text.Should().Contain(InstitutionName)
            .And.Contain("Plan de estudios 2026")
            .And.Contain("Sede Principal")
            .And.Contain("RESPONSABLE: CONSEJO ACADÉMICO")
            .And.Contain("NIVELES: Básica")
            .And.Contain("ÁREAS ASIGNATURAS 6º 7º IH IH")
            .And.Contain("MATEMÁTICAS Aritmética 5 5")
            .And.Contain("Total semanal 22 22")
            .And.Contain("Total horas anuales (40 semanas) 880 880")
            .And.NotContain("Proyecto de investigación", "las asignaturas sin IH en ningún grado no salen")
            .And.NotContain("CIENCIAS SOCIALES", "un área sin asignaturas en el plan no sale");
    }

    [Fact]
    public async Task StudyPlanPdf_ShouldMarkTransversalAndCounterShiftSubjectsWithAnAsteriskAndExplainThem()
    {
        var school = await SeedAsync(s =>
        {
            s.Plan.AddItem(s.Sixth.Id, s.Citizenship.Id, DeliveryMode.Transversal, 0, null, s.Spanish.Id).IsError.Should().BeFalse();
            s.Plan.AddItem(s.Seventh.Id, s.Citizenship.Id, DeliveryMode.Transversal, 0, null, s.Spanish.Id).IsError.Should().BeFalse();
            s.Plan.AddItem(s.Seventh.Id, s.Research.Id, DeliveryMode.CounterShift, 2, s.Afternoon.Id).IsError.Should().BeFalse();
            s.Plan.AddItem(s.Sixth.Id, s.Research.Id, DeliveryMode.CounterShift, 2, s.Afternoon.Id, note: "Se trabaja por proyectos").IsError.Should().BeFalse();
            s.Plan.Update("Plan de prueba", "Aprobado por el consejo académico.\nVigente desde enero.").IsError.Should().BeFalse();
        });

        var text = PdfPages((await _host.SendAsync(new GetStudyPlanDocumentQuery(school.Plan.Id.Value, ReportFormat.Pdf))).Value)[0];

        text.Should().Contain("CIENCIAS SOCIALES Competencia ciudadana * *")
            .And.Contain("Proyecto de investigación 2* 2*")
            .And.Contain("* Competencia ciudadana (6º y 7º) se trabaja como eje transversal en Lengua castellana.")
            .And.Contain("* Proyecto de investigación (6º): Se trabaja por proyectos")
            .And.Contain("* Proyecto de investigación (7º) se desarrolla en contrajornada (Tarde).")
            .And.Contain("En contrajornada 2 2")
            .And.Contain("Total semanal 22 22", "transversales y contrajornada no suman al total de la jornada")
            .And.Contain("Aprobado por el consejo académico.")
            .And.Contain("Vigente desde enero.");
    }

    [Fact]
    public async Task StudyPlanForAPeriod_ShouldUseThePeriodHoursAndTotals()
    {
        var school = await SeedAsync(s =>
            s.Plan.SetItemPeriodHours(s.Item(s.Sixth, s.Arithmetic).Id, s.Period2.Id, 3).IsError.Should().BeFalse());

        var general = PdfPages((await _host.SendAsync(new GetStudyPlanDocumentQuery(school.Plan.Id.Value, ReportFormat.Pdf))).Value)[0];
        var period2 = await _host.SendAsync(new GetStudyPlanDocumentQuery(school.Plan.Id.Value, ReportFormat.Pdf, school.Period2.Id.Value));
        var otherYear = await _host.SendAsync(new GetStudyPlanDocumentQuery(school.Plan.Id.Value, ReportFormat.Pdf, Guid.NewGuid()));

        general.Should().Contain("Aritmética 5 5").And.Contain("Total semanal 22 22");
        period2.Value.FileName.Should().Be("Plan de prueba - Periodo 2.pdf");
        var text = PdfPages(period2.Value)[0];
        text.Should().Contain("Plan de estudios 2026 · Periodo 2")
            .And.Contain("Aritmética 3 5")
            .And.Contain("Total semanal 20 22")
            .And.Contain("800 880");
        otherYear.FirstError.Should().Be(StudyPlanErrors.PeriodNotInYear);
    }

    [Fact]
    public async Task StudyPlan_WithTrainingProjects_ShouldAddAPageWithTheCampusProjectsGroupedByCategory()
    {
        var school = await SeedAsync(withProjects: true);

        var pages = PdfPages((await _host.SendAsync(new GetStudyPlanDocumentQuery(school.Plan.Id.Value, ReportFormat.Pdf))).Value);

        pages.Should().HaveCount(2);
        pages[1].Should().Contain("Proyectos de formación")
            .And.Contain("Artículo 36, Decreto 1860 de 1994")
            .And.Contain("PROYECTOS NOMBRE RESPONSABLE")
            .And.Contain("INSTITUCIONALES FESTIVAL DE PORRAS Fabio Deportes")
            .And.Contain("OBLIGATORIOS EDUCACIÓN PARA LA DEMOCRACIA Consejo estudiantil")
            .And.NotContain("HUERTA ESCOLAR", "es de otra sede");
    }

    [Fact]
    public async Task StudyPlanWord_ShouldBeValidAndHaveTheSameTableWithMergedAreaCells()
    {
        var school = await SeedAsync(withProjects: true);

        var result = await _host.SendAsync(new GetStudyPlanDocumentQuery(school.Plan.Id.Value, ReportFormat.Word));

        result.Value.FileName.Should().Be("Plan de prueba.docx");
        using var word = OpenWord(result.Value);
        var body = word.MainDocumentPart!.Document!.Body!;
        body.InnerText.Should().Contain(InstitutionName).And.Contain("RESPONSABLE: CONSEJO ACADÉMICO");

        var tables = body.Elements<Table>().ToList();
        tables.Should().HaveCount(2, "el plan y los proyectos de formación");
        body.Descendants<Break>().Count(b => b.Type?.Value == BreakValues.Page).Should().Be(1, "los proyectos van en otra página");

        var plan = tables[0];
        var rows = WordTable(plan);
        rows[0].Should().Equal("ÁREAS", "ASIGNATURAS", "6º", "7º");
        rows[1].Should().Equal("", "", "IH", "IH", "las dos primeras columnas vienen combinadas de la fila anterior");
        plan.Elements<TableRow>().Take(2).Should().OnlyContain(r => r.TableRowProperties!.GetFirstChild<TableHeader>() != null,
            "el encabezado se repite en cada página");

        rows.Should().ContainEquivalentOf(new List<string> { "HUMANIDADES", "Lengua castellana", "5", "5" });
        rows.Should().ContainEquivalentOf(new List<string> { "", "Inglés", "4", "4" }, "el área ocupa varias filas");
        rows[^2].Should().Equal("Total semanal", "22", "22");
        rows[^1].Should().Equal("Total horas anuales (40 semanas)", "880", "880");

        // Combinan filas: ÁREAS y ASIGNATURAS en el encabezado, y Humanidades (la única área con dos asignaturas)
        var restarts = plan.Descendants<VerticalMerge>().Count(v => v.Val?.Value == MergedCellValues.Restart);
        restarts.Should().Be(3);
        plan.Descendants<GridSpan>().Should().OnlyContain(g => g.Val! == 2, "los totales ocupan las dos primeras columnas");
    }

    [Fact]
    public async Task StudyPlanDocument_ForAMissingPlan_ShouldReturnNotFound()
    {
        await SeedAsync();

        var result = await _host.SendAsync(new GetStudyPlanDocumentQuery(Guid.NewGuid(), ReportFormat.Word));

        result.FirstError.Should().Be(StudyPlanErrors.NotFound);
    }

    #endregion

    #region Horarios

    private async Task<TestSchool> SeedGeneratedAsync()
    {
        var school = await SeedAsync();
        var job = GenerationJob.Create(school.Timetable.Id, DateTimeOffset.UtcNow, 30);
        await _host.SeedAsync(db => db.GenerationJobs.Add(job));
        await _host.RunJobAsync(job.Id);
        var status = await _host.WithDbAsync(db => db.GenerationJobs.Where(j => j.Id == job.Id).Select(j => j.Status).FirstAsync());
        status.Should().Be(GenerationJobStatus.Succeeded);
        return school;
    }

    [Fact]
    public async Task TimetableByCourse_ShouldHaveOnePagePerCourseWithEveryPeriodAndDay()
    {
        var school = await SeedGeneratedAsync();

        var result = await _host.SendAsync(new GetTimetableDocumentQuery(school.Timetable.Id.Value, TimetableDocumentView.Courses, ReportFormat.Pdf));

        result.Value.FileName.Should().Be("Horario de prueba - por curso.pdf");
        var pages = PdfPages(result.Value);
        pages.Should().HaveCount(4);
        new[] { "6ºA", "6ºB", "7ºA", "7ºB" }.Zip(pages).Should().OnlyContain(p => p.Second.Contains($"Horario de {p.First}"));
        pages.Should().OnlyContain(p =>
            p.Contains("HORA LUNES MARTES MIÉRCOLES JUEVES VIERNES") &&
            p.Contains("SEDE: Sede Principal") &&
            p.Contains("PERIODO: Periodo 1 2026") &&
            p.Contains("ESTADO: Borrador") &&
            Enumerable.Range(1, TestSchool.MorningPeriods).All(n => p.Contains($"{n}ª hora")) &&
            p.Contains("7:00–7:50"));
    }

    [Fact]
    public async Task TimetableByCourse_ForOneCourse_ShouldHaveThatCoursesLessons()
    {
        var school = await SeedGeneratedAsync();
        var lessons = await _host.WithDbAsync(db =>
            db.Timetables.Where(t => t.Id == school.Timetable.Id).SelectMany(t => t.Lessons).Where(l => l.CourseId == school.C7B.Id).ToListAsync());

        var result = await _host.SendAsync(new GetTimetableDocumentQuery(school.Timetable.Id.Value, TimetableDocumentView.Courses, ReportFormat.Word,
            CourseId: school.C7B.Id.Value));

        result.Value.FileName.Should().Be("Horario de prueba - Horario de 7ºB.docx");
        using var word = OpenWord(result.Value);
        var table = word.MainDocumentPart!.Document!.Body!.Elements<Table>().Single();
        var rows = WordTable(table);
        rows[0].Should().Equal("HORA", "LUNES", "MARTES", "MIÉRCOLES", "JUEVES", "VIERNES");
        rows.Should().HaveCount(1 + TestSchool.MorningPeriods);

        // Cada clase del curso aparece en su día y franja, con asignatura, docente y salón
        var subjects = await _host.WithDbAsync(db => db.Areas.SelectMany(a => a.Subjects).ToDictionaryAsync(s => s.Id, s => s.Name));
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        foreach (var lesson in lessons)
            rows[lesson.PeriodNumber][Array.IndexOf(days, lesson.Day) + 1].Should().StartWith(subjects[lesson.SubjectId]);
        rows.Skip(1).SelectMany(r => r.Skip(1)).Count(c => c.Length > 0).Should().Be(lessons.Count).And.Be(TestSchool.BaseWeeklyHours);
    }

    [Fact]
    public async Task TimetableByTeacher_ShouldHaveOnePagePerTeacherWithTheirCourses()
    {
        var school = await SeedGeneratedAsync();
        var teachers = await _host.WithDbAsync(db =>
            db.Timetables.Where(t => t.Id == school.Timetable.Id).SelectMany(t => t.Lessons).Select(l => l.TeacherId).Distinct().CountAsync());

        var pages = PdfPages((await _host.SendAsync(
            new GetTimetableDocumentQuery(school.Timetable.Id.Value, TimetableDocumentView.Teachers, ReportFormat.Pdf))).Value);

        pages.Should().HaveCount(teachers);
        pages.Should().Contain(p => p.Contains("Horario de Ana Matemáticas") && p.Contains("Aritmética") && p.Contains("6ºA"));
    }

    [Fact]
    public async Task InstitutionalTimetable_ShouldHaveOnePagePerDayWithACourseColumnEach()
    {
        var school = await SeedGeneratedAsync();

        var result = await _host.SendAsync(new GetTimetableDocumentQuery(school.Timetable.Id.Value, TimetableDocumentView.Days, ReportFormat.Word));

        using var word = OpenWord(result.Value);
        var body = word.MainDocumentPart!.Document!.Body!;
        var tables = body.Elements<Table>().ToList();
        tables.Should().HaveCount(5);
        body.InnerText.Should().Contain("Horario institucional · Lunes").And.Contain("Horario institucional · Viernes");
        tables.Should().OnlyContain(t => WordTable(t)[0].SequenceEqual(new[] { "HORA", "6ºA", "6ºB", "7ºA", "7ºB" }));
        tables.Sum(t => WordTable(t).Skip(1).SelectMany(r => r.Skip(1)).Count(c => c.Length > 0)).Should().Be(4 * TestSchool.BaseWeeklyHours);
    }

    [Fact]
    public async Task TimetableDocument_ForTeachers_OnlyWhenPublished_AndNeverEmpty()
    {
        var school = await SeedAsync();

        var empty = await _host.SendAsync(new GetTimetableDocumentQuery(school.Timetable.Id.Value, TimetableDocumentView.Courses, ReportFormat.Pdf));
        empty.FirstError.Should().Be(TimetableErrors.Empty);

        _host.SignInAs(Roles.Teacher, teacherId: school.MathTeacher.Id);
        var draft = await _host.SendAsync(new GetTimetableDocumentQuery(school.Timetable.Id.Value, TimetableDocumentView.Teachers, ReportFormat.Pdf));
        draft.FirstError.Should().Be(TimetableErrors.NotFound, "el docente no ve borradores");
    }

    #endregion
}
