using Microsoft.AspNetCore.Mvc;
using STG.Api.DTOs;
using STG.Application.Services;
using STG.Domain.Entities;
using STG.Domain.ValueObjects;
using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

[ApiController]
[Route("api/[controller]")]
public class SchedulerController : ControllerBase
{
    private readonly SchedulerService _svc;
    public SchedulerController(SchedulerService svc) => _svc = svc;

    [HttpPost("run/{year:int}")]
    public async Task<ActionResult<TimetableResponse>> Run(int year, [FromBody] WeekRequest req, CancellationToken ct)
    {
        var week = new WeekConfig(
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            req.BlocksPerDay,
            req.BlockLengthMinutes
        );

        var result = await _svc.GenerateAsync(year, week, ct);
        var dto = new TimetableResponse(
            result.Id,
            result.Year,
            result.Assignments.Select(a =>
                new AssignmentResponse(
                    a.GroupCode,
                    a.Subject,
                    a.Teacher,
                    a.Room,
                    a.Slot.Day.ToString(),
                    a.Slot.Block
                )).ToList()
        );
        return Ok(dto);
    }


    [HttpPost("export")]
    public async Task<IActionResult> Export([FromQuery] string format, [FromBody] ExportRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(format))
            return BadRequest(new ProblemDetails { Title = "Missing format", Detail = "Use ?format=csv|xls|pdf" });

        format = format.Trim().ToLowerInvariant();
        if (format is not ("csv" or "xls" or "pdf"))
            return BadRequest(new ProblemDetails { Title = "Invalid format", Detail = "Valid: csv, xls, pdf" });

        // Obtener o generar el horario del año solicitado
        // Para evitar regenerar, podrías consultar primero por year; aquí usamos GenerateAsync si no existe.
        var week = new STG.Domain.ValueObjects.WeekConfig(
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            blocksPerDay: 7,
            blockLengthMinutes: 45
        );

        var timetable = await _svc.GenerateAsync(req.Year, week, ct);

        // Filtrar
        var rows = FilterAssignments(timetable, req);

        // Despachar por formato
        var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
        var fileNameBase = $"timetable_{req.Year}_{ts}";

        return format switch
        {
            "csv" => File(ExportCsv(rows), "text/csv", $"{fileNameBase}.csv"),
            "xls" => File(ExportXlsx(rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{fileNameBase}.xlsx"),
            "pdf" => File(ExportPdf(rows), "application/pdf", $"{fileNameBase}.pdf"),
            _ => BadRequest(new ProblemDetails { Title = "Invalid format" })
        };
    }

    // ---- Helpers ----

    private sealed record Row(string GroupCode, string Subject, string Teacher, string Room, string Day, int Block);

    private static List<Row> FilterAssignments(Timetable timetable, ExportRequest req)
    {
        var q = timetable.Assignments.AsEnumerable();

        if (req.Grades is { Count: > 0 })
            q = q.Where(a => req.Grades!.Any(g => a.GroupCode.StartsWith(g, StringComparison.OrdinalIgnoreCase)));

        if (req.Groups is { Count: > 0 })
            q = q.Where(a => req.Groups!.Contains(a.GroupCode, StringComparer.OrdinalIgnoreCase));

        return q
            .OrderBy(a => a.GroupCode)
            .ThenBy(a => a.Slot.Day)
            .ThenBy(a => a.Slot.Block)
            .Select(a => new Row(
                a.GroupCode,
                a.Subject,
                a.Teacher,
                a.Room,
                a.Slot.Day.ToString(),
                a.Slot.Block
            ))
            .ToList();
    }

    private static byte[] ExportCsv(List<Row> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Group,Subject,Teacher,Room,Day,Block");
        foreach (var r in rows)
            sb.AppendLine($"{Escape(r.GroupCode)},{Escape(r.Subject)},{Escape(r.Teacher)},{Escape(r.Room)},{Escape(r.Day)},{r.Block}");
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());

        static string Escape(string v)
        {
            if (v.Contains(',') || v.Contains('"'))
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            return v;
        }
    }

    private static byte[] ExportXlsx(List<Row> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Timetable");

        // Header
        ws.Cell(1, 1).Value = "Group";
        ws.Cell(1, 2).Value = "Subject";
        ws.Cell(1, 3).Value = "Teacher";
        ws.Cell(1, 4).Value = "Room";
        ws.Cell(1, 5).Value = "Day";
        ws.Cell(1, 6).Value = "Block";
        ws.Range(1, 1, 1, 6).Style.Font.Bold = true;

        // Rows
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            int rr = i + 2;
            ws.Cell(rr, 1).Value = r.GroupCode;
            ws.Cell(rr, 2).Value = r.Subject;
            ws.Cell(rr, 3).Value = r.Teacher;
            ws.Cell(rr, 4).Value = r.Room;
            ws.Cell(rr, 5).Value = r.Day;
            ws.Cell(rr, 6).Value = r.Block;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] ExportPdf(List<Row> rows)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var bytes = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Text("Smart Timetable Generator - Export").SemiBold().FontSize(16);
                page.Content().Table(table =>
                {
                    // columns
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1); // Group
                        cols.RelativeColumn(2); // Subject
                        cols.RelativeColumn(2); // Teacher
                        cols.RelativeColumn(1); // Room
                        cols.RelativeColumn(1); // Day
                        cols.RelativeColumn(1); // Block
                    });

                    // header row
                    table.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text("Group");
                        h.Cell().Element(HeaderCell).Text("Subject");
                        h.Cell().Element(HeaderCell).Text("Teacher");
                        h.Cell().Element(HeaderCell).Text("Room");
                        h.Cell().Element(HeaderCell).Text("Day");
                        h.Cell().Element(HeaderCell).Text("Block");

                        static IContainer HeaderCell(IContainer c) => c.Padding(4).Background(Colors.Grey.Lighten3).DefaultTextStyle(x => x.SemiBold());
                    });

                    // data rows
                    foreach (var r in rows)
                    {
                        table.Cell().Element(Cell).Text(r.GroupCode);
                        table.Cell().Element(Cell).Text(r.Subject);
                        table.Cell().Element(Cell).Text(r.Teacher);
                        table.Cell().Element(Cell).Text(r.Room);
                        table.Cell().Element(Cell).Text(r.Day);
                        table.Cell().Element(Cell).Text(r.Block.ToString());

                        static IContainer Cell(IContainer c) => c.Padding(3);
                    }
                });
                page.Footer().AlignRight().Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            });
        })
        .GeneratePdf();

        return bytes;
    }
}
