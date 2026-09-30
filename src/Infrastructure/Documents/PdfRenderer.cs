using QuestPDF.Elements.Table;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartTimetableGenerator.Application.Common.Documents;

namespace SmartTimetableGenerator.Infrastructure.Documents;

/// <summary>
/// PDF con QuestPDF (licencia Community: gratuita para organizaciones con ingresos anuales menores a 1 millón de dólares).
/// </summary>
internal static class PdfRenderer
{
    static PdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Render(ReportDocument document) =>
        Document.Create(container =>
        {
            foreach (var section in document.Sections)
            {
                container.Page(page =>
                {
                    page.Size(document.Landscape ? PageSizes.Letter.Landscape() : PageSizes.Letter);
                    page.Margin(1.2f, Unit.Centimetre);
                    page.DefaultTextStyle(style => style.FontSize(8.5f));

                    page.Header().PaddingBottom(6).Column(column =>
                    {
                        column.Item().Text(document.InstitutionName).FontSize(12).Bold();
                        column.Item().Text(section.Title).FontSize(11).SemiBold().FontColor(Colors.Blue.Darken3);
                        if (section.Subtitle is not null)
                            column.Item().Text(section.Subtitle).FontColor(Colors.Grey.Darken1);
                        foreach (var field in section.Fields)
                        {
                            column.Item().Text(text =>
                            {
                                text.Span($"{field.Label}: ").Bold();
                                text.Span(field.Value);
                            });
                        }
                    });

                    page.Content().Column(column =>
                    {
                        column.Spacing(8);
                        foreach (var table in section.Tables)
                            column.Item().Element(c => DrawTable(c, table));
                        foreach (var note in section.Notes)
                            column.Item().Text(note).FontSize(8);
                        foreach (var paragraph in section.Paragraphs)
                            column.Item().Text(paragraph).FontSize(8.5f);
                    });

                    page.Footer().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(7).FontColor(Colors.Grey.Darken1));
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            }
        }).GeneratePdf();

    private static void DrawTable(IContainer container, ReportTable table)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(columns =>
            {
                foreach (var width in table.ColumnWidths)
                    columns.RelativeColumn(width);
            });

            if (table.HeaderRows.Count > 0)
            {
                t.Header(header =>
                {
                    foreach (var row in table.HeaderRows)
                        foreach (var cell in row.Cells)
                            DrawCell(header.Cell(), cell);
                });
            }

            foreach (var row in table.Rows)
                foreach (var cell in row.Cells)
                    DrawCell(t.Cell(), cell);
        });
    }

    private static void DrawCell(ITableCellContainer target, ReportCell cell)
    {
        var container = target
            .RowSpan((uint)Math.Max(1, cell.RowSpan))
            .ColumnSpan((uint)Math.Max(1, cell.ColumnSpan))
            .Border(0.5f)
            .BorderColor(Colors.Grey.Medium)
            .Background(cell.Style switch
            {
                CellStyle.Header => Colors.Blue.Lighten4,
                CellStyle.Total => Colors.Grey.Lighten3,
                CellStyle.Group => Colors.Grey.Lighten4,
                _ => Colors.White
            })
            .Padding(3);

        container = cell.Alignment == CellAlignment.Center ? container.AlignCenter().AlignMiddle() : container.AlignLeft().AlignMiddle();

        container.Column(column =>
        {
            for (var i = 0; i < cell.Lines.Count; i++)
            {
                var text = column.Item().Text(cell.Lines[i]);
                var bold = cell.Style is CellStyle.Header or CellStyle.Group or CellStyle.Total || (cell.EmphasizeFirstLine && i == 0);
                if (bold)
                    text.Bold();
                if (cell.Style == CellStyle.Muted || (cell.EmphasizeFirstLine && i > 0))
                    text.FontSize(7.5f).FontColor(Colors.Grey.Darken2);
            }
        });
    }
}
