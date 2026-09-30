using System.Text;
using SmartTimetableGenerator.Application.Common.Documents;
using SmartTimetableGenerator.Application.Common.Interfaces;

namespace SmartTimetableGenerator.Infrastructure.Documents;

/// <summary>
/// Genera el archivo en el formato pedido: Word (.docx, con OpenXml) o PDF (con QuestPDF).
/// </summary>
public sealed class DocumentRenderer : IDocumentRenderer
{
    public const string PdfContentType = "application/pdf";
    public const string WordContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public DocumentFile Render(ReportDocument document, ReportFormat format) =>
        format == ReportFormat.Word
            ? new DocumentFile(WordRenderer.Render(document), WordContentType, SafeFileName(document.FileName) + ".docx")
            : new DocumentFile(PdfRenderer.Render(document), PdfContentType, SafeFileName(document.FileName) + ".pdf");

    /// <summary>Quita los caracteres que no se permiten en nombres de archivo (Windows incluido).</summary>
    internal static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(['<', '>', ':', '"', '/', '\\', '|', '?', '*']).ToHashSet();
        var builder = new StringBuilder(name.Length);
        foreach (var ch in name)
            builder.Append(invalid.Contains(ch) || char.IsControl(ch) ? '-' : ch);
        var result = builder.ToString().Trim(' ', '.');
        return string.IsNullOrEmpty(result) ? "documento" : result;
    }
}

/// <summary>
/// Posiciones reales de las celdas de una tabla (resuelve RowSpan/ColumnSpan): para cada fila, en orden de columna,
/// la celda que empieza ahí o la continuación de una celda que viene de filas anteriores.
/// </summary>
internal static class CellGrid
{
    internal sealed record Slot(ReportCell Cell, int Column, bool IsContinuation);

    public static List<List<Slot>> Resolve(IReadOnlyList<ReportRow> rows, int columnCount)
    {
        var pending = new (ReportCell Cell, int Remaining)?[columnCount];
        var result = new List<List<Slot>>(rows.Count);

        foreach (var row in rows)
        {
            var slots = new List<Slot>();
            var queue = new Queue<ReportCell>(row.Cells);
            var column = 0;
            while (column < columnCount)
            {
                if (pending[column] is { } carried)
                {
                    slots.Add(new Slot(carried.Cell, column, IsContinuation: true));
                    pending[column] = carried.Remaining > 1 ? (carried.Cell, carried.Remaining - 1) : null;
                    column += Math.Max(1, carried.Cell.ColumnSpan);
                    continue;
                }

                if (queue.Count == 0)
                    break;

                var cell = queue.Dequeue();
                slots.Add(new Slot(cell, column, IsContinuation: false));
                if (cell.RowSpan > 1)
                    pending[column] = (cell, cell.RowSpan - 1);
                column += Math.Max(1, cell.ColumnSpan);
            }

            result.Add(slots);
        }

        return result;
    }
}
