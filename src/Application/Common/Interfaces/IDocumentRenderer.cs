using SmartTimetableGenerator.Application.Common.Documents;

namespace SmartTimetableGenerator.Application.Common.Interfaces;

/// <summary>
/// Convierte un <see cref="ReportDocument"/> en un archivo Word (.docx) o PDF.
/// </summary>
public interface IDocumentRenderer
{
    DocumentFile Render(ReportDocument document, ReportFormat format);
}
