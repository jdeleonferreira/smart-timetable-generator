using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SmartTimetableGenerator.Application.Common.Documents;

namespace SmartTimetableGenerator.Infrastructure.Documents;

/// <summary>
/// Word (.docx) con Open XML SDK: tablas con celdas combinadas, encabezados que se repiten en cada página
/// y una página por sección.
/// </summary>
internal static class WordRenderer
{
    // Tamaños en medios puntos (Word)
    private const string BodySize = "17";
    private const string SmallSize = "15";

    // Carta horizontal en veinteavos de punto (twips): 11 x 8,5 pulgadas, márgenes de 0,5 pulgadas
    private const int LetterLong = 15840;
    private const int LetterShort = 12240;
    private const int Margin = 720;

    public static byte[] Render(ReportDocument document)
    {
        using var stream = new MemoryStream();
        using (var word = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = word.AddMainDocumentPart();
            var body = new Body();
            var pageWidth = (document.Landscape ? LetterLong : LetterShort) - (2 * Margin);

            for (var index = 0; index < document.Sections.Count; index++)
            {
                var section = document.Sections[index];
                if (index > 0)
                    body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

                body.Append(TextParagraph(document.InstitutionName, bold: true, size: "24"));
                body.Append(TextParagraph(section.Title, bold: true, size: "22", color: "1F3A68"));
                if (section.Subtitle is not null)
                    body.Append(TextParagraph(section.Subtitle, size: BodySize, color: "666666"));
                foreach (var field in section.Fields)
                {
                    body.Append(new Paragraph(
                        Spacing(),
                        TextRun($"{field.Label}: ", bold: true, size: BodySize),
                        TextRun(field.Value, size: BodySize)));
                }

                foreach (var table in section.Tables)
                {
                    body.Append(TextParagraph("", size: SmallSize));
                    body.Append(BuildTable(table, pageWidth));
                }

                if (section.Notes.Count > 0 || section.Paragraphs.Count > 0)
                    body.Append(TextParagraph("", size: SmallSize));
                foreach (var note in section.Notes)
                    body.Append(TextParagraph(note, size: SmallSize));
                foreach (var paragraph in section.Paragraphs)
                    body.Append(TextParagraph(paragraph, size: BodySize));
            }

            body.Append(new SectionProperties(
                new PageSize
                {
                    Width = (UInt32Value)(uint)(document.Landscape ? LetterLong : LetterShort),
                    Height = (UInt32Value)(uint)(document.Landscape ? LetterShort : LetterLong),
                    Orient = document.Landscape ? PageOrientationValues.Landscape : PageOrientationValues.Portrait
                },
                new PageMargin
                {
                    Top = Margin,
                    Right = Margin,
                    Bottom = Margin,
                    Left = Margin,
                    Header = 360U,
                    Footer = 360U,
                    Gutter = 0U
                }));

            main.Document = new Document(body);
            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static Table BuildTable(ReportTable table, int pageWidth)
    {
        var columnCount = table.ColumnWidths.Count;
        var totalWeight = table.ColumnWidths.Sum();
        var widths = table.ColumnWidths.Select(w => (int)(pageWidth * w / totalWeight)).ToArray();

        var result = new Table(
            new TableProperties(
                new TableWidth { Width = pageWidth.ToString(System.Globalization.CultureInfo.InvariantCulture), Type = TableWidthUnitValues.Dxa },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "999999" }),
                new TableLayout { Type = TableLayoutValues.Fixed },
                new TableCellMarginDefault(
                    new TopMargin { Width = "30", Type = TableWidthUnitValues.Dxa },
                    new TableCellLeftMargin { Width = 60, Type = TableWidthValues.Dxa },
                    new BottomMargin { Width = "30", Type = TableWidthUnitValues.Dxa },
                    new TableCellRightMargin { Width = 60, Type = TableWidthValues.Dxa })),
            new TableGrid(widths.Select(w => new GridColumn { Width = w.ToString(System.Globalization.CultureInfo.InvariantCulture) })));

        AppendRows(result, CellGrid.Resolve(table.HeaderRows, columnCount), widths, isHeader: true);
        AppendRows(result, CellGrid.Resolve(table.Rows, columnCount), widths, isHeader: false);
        return result;
    }

    private static void AppendRows(Table table, List<List<CellGrid.Slot>> rows, int[] widths, bool isHeader)
    {
        foreach (var slots in rows)
        {
            var row = new TableRow();
            if (isHeader)
                row.Append(new TableRowProperties(new CantSplit(), new TableHeader()));
            else
                row.Append(new TableRowProperties(new CantSplit()));

            foreach (var slot in slots)
                row.Append(Cell(slot, widths));

            table.Append(row);
        }
    }

    private static TableCell Cell(CellGrid.Slot slot, int[] widths)
    {
        var cell = slot.Cell;
        var span = Math.Max(1, cell.ColumnSpan);
        var width = widths.Skip(slot.Column).Take(span).Sum();

        var properties = new TableCellProperties(
            new TableCellWidth { Width = width.ToString(System.Globalization.CultureInfo.InvariantCulture), Type = TableWidthUnitValues.Dxa });
        if (span > 1)
            properties.Append(new GridSpan { Val = span });
        if (cell.RowSpan > 1 || slot.IsContinuation)
            properties.Append(new VerticalMerge { Val = slot.IsContinuation ? MergedCellValues.Continue : MergedCellValues.Restart });

        var fill = cell.Style switch
        {
            CellStyle.Header => "DCE6F4",
            CellStyle.Total => "EEEEEE",
            CellStyle.Group => "F5F5F5",
            _ => null
        };
        if (fill is not null)
            properties.Append(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = fill });
        properties.Append(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });

        var result = new TableCell(properties);
        if (slot.IsContinuation || cell.Lines.Count == 0)
        {
            result.Append(new Paragraph(Spacing()));
            return result;
        }

        for (var i = 0; i < cell.Lines.Count; i++)
        {
            var bold = cell.Style is CellStyle.Header or CellStyle.Group or CellStyle.Total || (cell.EmphasizeFirstLine && i == 0);
            var secondary = cell.Style == CellStyle.Muted || (cell.EmphasizeFirstLine && i > 0);
            var paragraph = new Paragraph(Spacing(cell.Alignment == CellAlignment.Center));
            paragraph.Append(TextRun(cell.Lines[i], bold, secondary ? SmallSize : BodySize, secondary ? "555555" : null));
            result.Append(paragraph);
        }

        return result;
    }

    private static ParagraphProperties Spacing(bool center = false)
    {
        var properties = new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0" });
        if (center)
            properties.Append(new Justification { Val = JustificationValues.Center });
        return properties;
    }

    private static Paragraph TextParagraph(string text, bool bold = false, string size = BodySize, string? color = null) =>
        new(Spacing(), TextRun(text, bold, size, color));

    private static Run TextRun(string text, bool bold = false, string size = BodySize, string? color = null)
    {
        var properties = new RunProperties(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", ComplexScript = "Calibri" });
        if (bold)
            properties.Append(new Bold());
        if (color is not null)
            properties.Append(new Color { Val = color });
        properties.Append(new FontSize { Val = size });

        return new Run(properties, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }
}
