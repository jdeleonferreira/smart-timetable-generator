using System.Globalization;
using SmartTimetableGenerator.Application.Common.Documents;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.UseCases.Timetables.Queries.GetTimetableLessons;
using SmartTimetableGenerator.Domain.Timetables;

namespace SmartTimetableGenerator.Application.UseCases.Documents.Queries.GetTimetableDocument;

/// <summary>
/// Cómo se organiza el documento del horario.
/// </summary>
public enum TimetableDocumentView
{
    /// <summary>Una página por curso (días en columnas).</summary>
    Courses = 0,

    /// <summary>Una página por docente (días en columnas).</summary>
    Teachers = 1,

    /// <summary>Horario institucional: una página por día (cursos en columnas).</summary>
    Days = 2
}

/// <summary>
/// Documento del horario: por curso, por docente o institucional por día. Con <see cref="CourseId"/> o
/// <see cref="TeacherId"/> sale solo esa página. Los docentes solo pueden descargar horarios publicados.
/// </summary>
public sealed record GetTimetableDocumentQuery(
    Guid TimetableId,
    TimetableDocumentView View,
    ReportFormat Format,
    Guid? CourseId = null,
    Guid? TeacherId = null) : IRequest<ErrorOr<DocumentFile>>;

internal sealed class GetTimetableDocumentQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser,
    IDocumentRenderer renderer)
    : IRequestHandler<GetTimetableDocumentQuery, ErrorOr<DocumentFile>>
{
    private static readonly DayOfWeek[] WeekOrder =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    internal static readonly IReadOnlyDictionary<DayOfWeek, string> DayNames = new Dictionary<DayOfWeek, string>
    {
        [DayOfWeek.Monday] = "Lunes",
        [DayOfWeek.Tuesday] = "Martes",
        [DayOfWeek.Wednesday] = "Miércoles",
        [DayOfWeek.Thursday] = "Jueves",
        [DayOfWeek.Friday] = "Viernes",
        [DayOfWeek.Saturday] = "Sábado",
        [DayOfWeek.Sunday] = "Domingo"
    };

    public async Task<ErrorOr<DocumentFile>> Handle(GetTimetableDocumentQuery request, CancellationToken cancellationToken)
    {
        // Mismas reglas de acceso que la consulta de clases (el docente solo ve horarios publicados)
        var lessonsResult = await new GetTimetableLessonsQueryHandler(dbContext, currentUser)
            .Handle(new GetTimetableLessonsQuery(request.TimetableId, null, null, null), cancellationToken);
        if (lessonsResult.IsError)
            return lessonsResult.Errors;
        var lessons = lessonsResult.Value;
        if (lessons.Count == 0)
            return TimetableErrors.Empty;

        var timetableId = TimetableId.From(request.TimetableId);
        var timetable = await dbContext.Timetables.AsNoTracking().FirstAsync(t => t.Id == timetableId, cancellationToken);
        var institution = await dbContext.Institutions.AsNoTracking().Select(i => i.Name).FirstOrDefaultAsync(cancellationToken);
        var campus = await dbContext.Campuses.AsNoTracking().Where(c => c.Id == timetable.CampusId).Select(c => c.Name).FirstAsync(cancellationToken);
        var year = await dbContext.AcademicYears.AsNoTracking().Include(y => y.Periods)
            .FirstAsync(y => y.Id == timetable.AcademicYearId, cancellationToken);
        var period = year.FindPeriod(timetable.AcademicPeriodId)?.Name ?? "";

        var grades = await dbContext.Grades.AsNoTracking().ToDictionaryAsync(g => g.Id, g => (g.Order, g.ShortName), cancellationToken);
        var courses = (await dbContext.Courses.AsNoTracking()
                .Where(c => c.CampusId == timetable.CampusId && c.AcademicYearId == timetable.AcademicYearId)
                .ToListAsync(cancellationToken))
            .OrderBy(c => grades[c.GradeId].Order).ThenBy(c => c.Name)
            .Select(c => (Id: c.Id.Value, Name: grades[c.GradeId].ShortName + c.Name))
            .ToList();

        var fields = new List<ReportField>
        {
            new("SEDE", campus),
            new("PERIODO", $"{period} {year.Year}"),
            new("ESTADO", timetable.Status == TimetableStatus.Published ? "Publicado" : "Borrador")
        };

        List<ReportSection> sections;
        string suffix;
        switch (request.View)
        {
            case TimetableDocumentView.Teachers:
                var teachers = lessons
                    .Where(l => l.TeacherId is not null && (request.TeacherId is null || l.TeacherId == request.TeacherId))
                    .GroupBy(l => (Id: l.TeacherId!.Value, Name: l.TeacherName ?? ""))
                    .OrderBy(g => g.Key.Name, StringComparer.CurrentCultureIgnoreCase);
                sections = teachers
                    .Select(g => WeekSection($"Horario de {g.Key.Name}", g.ToList(), fields,
                        l => [l.SubjectName, l.CourseName, l.SpaceName ?? ""]))
                    .ToList();
                suffix = request.TeacherId is null ? "por docente" : sections.FirstOrDefault()?.Title ?? "docente";
                break;

            case TimetableDocumentView.Days:
                var days = WeekOrder.Where(d => (d != DayOfWeek.Saturday && d != DayOfWeek.Sunday) || lessons.Any(l => l.Day == d));
                sections = days
                    .Select(day => DaySection(day, lessons.Where(l => l.Day == day).ToList(), courses, fields))
                    .ToList();
                suffix = "institucional";
                break;

            case TimetableDocumentView.Courses:
            default:
                sections = courses
                    .Where(c => request.CourseId is null || c.Id == request.CourseId)
                    .Select(c => WeekSection($"Horario de {c.Name}", lessons.Where(l => l.CourseId == c.Id).ToList(), fields,
                        l => [l.SubjectName, l.TeacherName ?? "", l.SpaceName ?? ""]))
                    .ToList();
                suffix = request.CourseId is null ? "por curso" : sections.FirstOrDefault()?.Title ?? "curso";
                break;
        }

        if (sections.Count == 0)
            return TimetableErrors.Empty;

        var document = new ReportDocument(institution ?? "Institución educativa", $"{timetable.Name} - {suffix}", sections);
        return renderer.Render(document, request.Format);
    }

    /// <summary>Fila por franja (agrupadas por jornada) y columna por día.</summary>
    private static ReportSection WeekSection(
        string title,
        List<LessonDto> lessons,
        List<ReportField> fields,
        Func<LessonDto, string[]> lines)
    {
        var days = WeekOrder.Where(d => (d != DayOfWeek.Saturday && d != DayOfWeek.Sunday) || lessons.Any(l => l.Day == d)).ToList();
        var header = new ReportRow([
            ReportCell.Text("HORA", CellStyle.Header, CellAlignment.Center),
            .. days.Select(d => ReportCell.Text(DayNames[d].ToUpperInvariant(), CellStyle.Header, CellAlignment.Center))
        ]);

        var rows = Slots(lessons, days.Count, (slot, cells) =>
            cells.AddRange(days.Select(d => LessonCell(slot.Where(l => l.Day == d), lines))));

        return new ReportSection(title, null, fields, [new ReportTable([1.3f, .. days.Select(_ => 2f)], [header], rows)], [], []);
    }

    /// <summary>Horario institucional de un día: fila por franja, columna por curso.</summary>
    private static ReportSection DaySection(DayOfWeek day, List<LessonDto> lessons, List<(Guid Id, string Name)> courses, List<ReportField> fields)
    {
        var header = new ReportRow([
            ReportCell.Text("HORA", CellStyle.Header, CellAlignment.Center),
            .. courses.Select(c => ReportCell.Text(c.Name, CellStyle.Header, CellAlignment.Center))
        ]);

        var rows = Slots(lessons, courses.Count, (slot, cells) =>
            cells.AddRange(courses.Select(c => LessonCell(slot.Where(l => l.CourseId == c.Id),
                l => [l.SubjectCode ?? l.SubjectName, ShortName(l.TeacherName)]))));

        return new ReportSection($"Horario institucional · {DayNames[day]}", null, fields,
            [new ReportTable([1.3f, .. courses.Select(_ => 1f)], [header], rows)], [], []);
    }

    private static List<ReportRow> Slots(List<LessonDto> lessons, int columns, Action<List<LessonDto>, List<ReportCell>> fill)
    {
        var rows = new List<ReportRow>();
        var shifts = lessons.GroupBy(l => l.ShiftName)
            .OrderBy(g => g.Min(l => l.Start ?? TimeOnly.MaxValue))
            .ToList();

        foreach (var shift in shifts)
        {
            if (shifts.Count > 1)
                rows.Add(new ReportRow([new ReportCell([$"Jornada {shift.Key}"], CellStyle.Group, ColumnSpan: columns + 1)]));

            foreach (var slot in shift.GroupBy(l => l.PeriodNumber).OrderBy(g => g.Key))
            {
                var first = slot.First();
                var time = first.Start is { } start && first.End is { } end
                    ? $"{start.ToString("H:mm", CultureInfo.InvariantCulture)}–{end.ToString("H:mm", CultureInfo.InvariantCulture)}"
                    : "";
                var cells = new List<ReportCell> { new([$"{slot.Key}ª hora", time], CellStyle.Normal, CellAlignment.Center, EmphasizeFirstLine: true) };
                fill(slot.ToList(), cells);
                rows.Add(new ReportRow(cells));
            }
        }

        return rows;
    }

    private static ReportCell LessonCell(IEnumerable<LessonDto> lessons, Func<LessonDto, string[]> lines)
    {
        var text = lessons.SelectMany(l => lines(l)).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        return new ReportCell(text, CellStyle.Normal, CellAlignment.Center, EmphasizeFirstLine: true);
    }

    private static string ShortName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1 ? parts[0] : $"{parts[0]} {parts[1][0]}.";
    }
}
