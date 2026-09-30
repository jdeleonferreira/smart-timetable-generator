using System.Globalization;
using SmartTimetableGenerator.Application.Common.Documents;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.UseCases.StudyPlans.Queries.GetStudyPlan;
using SmartTimetableGenerator.Domain.AcademicYears;
using SmartTimetableGenerator.Domain.Grades;
using SmartTimetableGenerator.Domain.StudyPlans;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Domain.TrainingProjects;

namespace SmartTimetableGenerator.Application.UseCases.Documents.Queries.GetStudyPlanDocument;

/// <summary>
/// Documento del plan de estudios con el formato institucional: encabezado (responsable, asunto, niveles), tabla de
/// áreas y asignaturas por grado con su IH, totales semanales y anuales, notas de las asignaturas marcadas con «*»
/// y, en otra página, los proyectos de formación del año. Con <see cref="AcademicPeriodId"/> muestra las IH de ese periodo.
/// </summary>
public sealed record GetStudyPlanDocumentQuery(Guid StudyPlanId, ReportFormat Format, Guid? AcademicPeriodId = null)
    : IRequest<ErrorOr<DocumentFile>>;

internal sealed class GetStudyPlanDocumentQueryHandler(IApplicationDbContext dbContext, IDocumentRenderer renderer)
    : IRequestHandler<GetStudyPlanDocumentQuery, ErrorOr<DocumentFile>>
{
    /// <summary>Semanas lectivas del año escolar (Ley 115 de 1994, art. 86).</summary>
    public const int SchoolWeeks = 40;

    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-CO");

    public async Task<ErrorOr<DocumentFile>> Handle(GetStudyPlanDocumentQuery request, CancellationToken cancellationToken)
    {
        var result = await new GetStudyPlanQueryHandler(dbContext).Handle(new GetStudyPlanQuery(request.StudyPlanId), cancellationToken);
        if (result.IsError)
            return result.Errors;

        var plan = result.Value;
        var periodId = request.AcademicPeriodId;
        var period = periodId is null ? null : plan.Periods.FirstOrDefault(p => p.Id == periodId);
        if (periodId is not null && period is null)
            return StudyPlanErrors.PeriodNotInYear;

        var institution = await dbContext.Institutions.AsNoTracking().Select(i => i.Name).FirstOrDefaultAsync(cancellationToken);
        var levels = await dbContext.Grades.AsNoTracking().ToDictionaryAsync(g => g.Id.Value, g => g.Level, cancellationToken);
        var yearId = AcademicYearId.From(plan.AcademicYearId);
        var projects = await dbContext.TrainingProjects.AsNoTracking()
            .Where(p => p.AcademicYearId == yearId)
            .ToListAsync(cancellationToken);
        var teachers = await dbContext.Teachers.AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.FirstName + " " + t.LastName, cancellationToken);

        var grades = plan.Grades.Where(g => HasItems(plan, g.Id)).ToList();
        if (grades.Count == 0)
            grades = plan.Grades.ToList();

        var sections = new List<ReportSection>
        {
            new(
                Title: $"Plan de estudios {plan.Year}" + (period is null ? "" : $" · {period.Name}"),
                Subtitle: plan.CampusName,
                Fields:
                [
                    new ReportField("RESPONSABLE", "CONSEJO ACADÉMICO"),
                    new ReportField("ASUNTO", "Esquema de planes de área y asignatura y proyectos de formación con sus respectivos tiempos"),
                    new ReportField("NIVELES", Levels(grades.Select(g => levels.GetValueOrDefault(g.Id))))
                ],
                Tables: [PlanTable(plan, grades, levels, periodId)],
                Notes: Notes(plan, grades),
                Paragraphs: SplitParagraphs(plan.Notes))
        };

        var campusProjects = projects
            .Where(p => p.CampusId is null || p.CampusId.Value.Value == plan.CampusId)
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .ToList();
        if (campusProjects.Count > 0)
            sections.Add(ProjectsSection(campusProjects, teachers));

        var fileName = $"{plan.Name}{(period is null ? "" : $" - {period.Name}")}";
        return renderer.Render(new ReportDocument(institution ?? "Institución educativa", fileName, sections), request.Format);
    }

    private static bool HasItems(StudyPlanDto plan, Guid gradeId) =>
        plan.Areas.SelectMany(a => a.Subjects).SelectMany(s => s.Items).Any(i => i.GradeId == gradeId);

    private static string GradeHeader(StudyPlanGradeDto grade, IReadOnlyDictionary<Guid, EducationLevel> levels) =>
        levels.GetValueOrDefault(grade.Id) == EducationLevel.Preschool ? grade.Name.ToUpper(Spanish) : grade.ShortName;

    private static string Levels(IEnumerable<EducationLevel> levels)
    {
        var names = levels.Distinct().Order()
            .Select(l => l switch
            {
                EducationLevel.Preschool => "Preescolar",
                EducationLevel.Primary or EducationLevel.LowerSecondary => "Básica",
                _ => "Media"
            })
            .Distinct()
            .ToList();
        return names.Count <= 1 ? string.Concat(names) : $"{string.Join(", ", names[..^1])} y {names[^1]}";
    }

    /// <summary>Texto de la celda: IH; «*» si es transversal; «IH*» si es en contrajornada.</summary>
    internal static string CellText(StudyPlanItemDto? item, Guid? periodId)
    {
        if (item is null)
            return "";

        if (item.DeliveryMode == DeliveryMode.Transversal)
            return "*";

        var hours = periodId is null
            ? item.WeeklyHours
            : item.PeriodHours.FirstOrDefault(p => p.AcademicPeriodId == periodId)?.WeeklyHours ?? item.WeeklyHours;
        return item.DeliveryMode == DeliveryMode.CounterShift ? $"{hours}*" : hours.ToString(Spanish);
    }

    private static ReportTable PlanTable(StudyPlanDto plan, List<StudyPlanGradeDto> grades, IReadOnlyDictionary<Guid, EducationLevel> levels, Guid? periodId)
    {
        var header = new List<ReportRow>
        {
            new([
                new ReportCell(["ÁREAS"], CellStyle.Header, CellAlignment.Center, RowSpan: 2),
                new ReportCell(["ASIGNATURAS"], CellStyle.Header, CellAlignment.Center, RowSpan: 2),
                .. grades.Select(g => ReportCell.Text(GradeHeader(g, levels), CellStyle.Header, CellAlignment.Center))
            ]),
            new([.. grades.Select(_ => ReportCell.Text("IH", CellStyle.Header, CellAlignment.Center))])
        };

        var rows = new List<ReportRow>();
        foreach (var area in plan.Areas)
        {
            var subjects = area.Subjects.Where(s => s.Items.Any(i => grades.Any(g => g.Id == i.GradeId))).ToList();
            for (var index = 0; index < subjects.Count; index++)
            {
                var subject = subjects[index];
                var cells = new List<ReportCell>();
                if (index == 0)
                    cells.Add(new ReportCell([area.Name.ToUpper(Spanish)], CellStyle.Group, RowSpan: subjects.Count));
                cells.Add(ReportCell.Text(subject.Name));
                cells.AddRange(grades.Select(g =>
                    ReportCell.Text(CellText(subject.Items.FirstOrDefault(i => i.GradeId == g.Id), periodId), alignment: CellAlignment.Center)));
                rows.Add(new ReportRow(cells));
            }
        }

        int Weekly(StudyPlanGradeDto g) =>
            periodId is null ? g.WeeklyTotal : g.PeriodTotals.FirstOrDefault(p => p.AcademicPeriodId == periodId)?.WeeklyTotal ?? g.WeeklyTotal;
        int CounterShift(StudyPlanGradeDto g) =>
            periodId is null ? g.CounterShiftTotal : g.PeriodTotals.FirstOrDefault(p => p.AcademicPeriodId == periodId)?.CounterShiftTotal ?? g.CounterShiftTotal;

        rows.Add(new ReportRow([
            new ReportCell(["Total semanal"], CellStyle.Total, ColumnSpan: 2),
            .. grades.Select(g => ReportCell.Text(Weekly(g).ToString(Spanish), CellStyle.Total, CellAlignment.Center))
        ]));
        if (grades.Any(g => CounterShift(g) > 0))
        {
            rows.Add(new ReportRow([
                new ReportCell(["En contrajornada"], CellStyle.Total, ColumnSpan: 2),
                .. grades.Select(g => ReportCell.Text(CounterShift(g) > 0 ? CounterShift(g).ToString(Spanish) : "", CellStyle.Total, CellAlignment.Center))
            ]));
        }
        rows.Add(new ReportRow([
            new ReportCell([$"Total horas anuales ({SchoolWeeks} semanas)"], CellStyle.Total, ColumnSpan: 2),
            .. grades.Select(g => ReportCell.Text((Weekly(g) * SchoolWeeks).ToString("N0", Spanish), CellStyle.Total, CellAlignment.Center))
        ]));

        return new ReportTable([2.4f, 3f, .. grades.Select(g => levels.GetValueOrDefault(g.Id) == EducationLevel.Preschool ? 1.5f : 1f)], header, rows);
    }

    /// <summary>
    /// Una nota por cada asignatura marcada con «*»: la nota del plan si la tiene; si no, una explicación según cómo se dicta.
    /// </summary>
    private static List<string> Notes(StudyPlanDto plan, List<StudyPlanGradeDto> grades)
    {
        var subjectNames = plan.Areas.SelectMany(a => a.Subjects).ToDictionary(s => s.Id, s => s.Name);
        var gradeNames = grades.ToDictionary(g => g.Id, g => g.ShortName);
        var shiftNames = plan.Shifts.ToDictionary(s => s.Id, s => s.Name);

        var notes = new List<string>();
        foreach (var subject in plan.Areas.SelectMany(a => a.Subjects))
        {
            var marked = subject.Items
                .Where(i => i.DeliveryMode != DeliveryMode.Regular && gradeNames.ContainsKey(i.GradeId))
                .GroupBy(i => (i.DeliveryMode, i.Note, i.IntegratedIntoSubjectId, i.TargetShiftId));

            foreach (var group in marked)
            {
                var gradeList = GradeRange(group.Select(i => gradeNames[i.GradeId]).ToList());
                var (mode, note, into, shift) = group.Key;

                var text = !string.IsNullOrWhiteSpace(note)
                    ? $"{subject.Name} ({gradeList}): {note}"
                    : mode == DeliveryMode.Transversal
                        ? $"{subject.Name} ({gradeList}) se trabaja como eje transversal" +
                          (into is { } i && subjectNames.TryGetValue(i, out var intoName) ? $" en {intoName}." : ".")
                        : $"{subject.Name} ({gradeList}) se desarrolla en contrajornada" +
                          (shift is { } s && shiftNames.TryGetValue(s, out var shiftName) ? $" ({shiftName})." : ".");
                notes.Add($"* {text}");
            }
        }

        return notes;
    }

    private static string GradeRange(List<string> grades) =>
        grades.Count switch
        {
            0 => "",
            1 => grades[0],
            2 => $"{grades[0]} y {grades[1]}",
            _ => $"{grades[0]} a {grades[^1]}"
        };

    private static List<string> SplitParagraphs(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static ReportSection ProjectsSection(List<TrainingProject> projects, Dictionary<TeacherId, string> teachers)
    {
        var rows = new List<ReportRow>();
        foreach (var group in projects.GroupBy(p => p.Category))
        {
            var list = group.ToList();
            for (var index = 0; index < list.Count; index++)
            {
                var project = list[index];
                var responsible = project.ResponsibleName ??
                                  (project.ResponsibleTeacherId is { } t && teachers.TryGetValue(t, out var name) ? name : "");
                var cells = new List<ReportCell>();
                if (index == 0)
                {
                    var category = group.Key == ProjectCategory.Mandatory ? "OBLIGATORIOS" : "INSTITUCIONALES";
                    cells.Add(new ReportCell([category], CellStyle.Group, RowSpan: list.Count));
                }
                cells.Add(ReportCell.Text(project.Name.ToUpper(Spanish)));
                cells.Add(ReportCell.Text(responsible));
                rows.Add(new ReportRow(cells));
            }
        }

        return new ReportSection(
            Title: "Proyectos de formación",
            Subtitle: "Artículo 36, Decreto 1860 de 1994",
            Fields: [],
            Tables:
            [
                new ReportTable([2f, 4f, 3f],
                    [new ReportRow([
                        ReportCell.Text("PROYECTOS", CellStyle.Header, CellAlignment.Center),
                        ReportCell.Text("NOMBRE", CellStyle.Header, CellAlignment.Center),
                        ReportCell.Text("RESPONSABLE", CellStyle.Header, CellAlignment.Center)
                    ])],
                    rows)
            ],
            Notes: [],
            Paragraphs:
            [
                "Los proyectos de formación correlacionan, integran y hacen activos los conocimientos, habilidades, actitudes y valores " +
                "logrados en las distintas áreas (artículo 14 de la Ley 115 de 1994). Se trabajan de manera transversal según el plan " +
                "operativo de cada proyecto."
            ]);
    }
}
