namespace SmartTimetableGenerator.Application.Common.Documents;

// Modelo neutro de un documento imprimible. Los casos de uso lo arman; Infrastructure lo convierte en Word o PDF.

public enum ReportFormat
{
    Pdf = 0,
    Word = 1
}

public enum CellStyle
{
    Normal = 0,
    /// <summary>Encabezado de tabla (fondo y negrita).</summary>
    Header = 1,
    /// <summary>Grupo, por ejemplo el nombre del área (negrita).</summary>
    Group = 2,
    /// <summary>Fila de totales (fondo y negrita).</summary>
    Total = 3,
    /// <summary>Texto secundario (gris).</summary>
    Muted = 4
}

public enum CellAlignment
{
    Left = 0,
    Center = 1
}

/// <summary>
/// Celda de tabla. <see cref="Lines"/> se muestran una debajo de otra (la primera en negrita si <see cref="EmphasizeFirstLine"/>).
/// </summary>
public sealed record ReportCell(
    IReadOnlyList<string> Lines,
    CellStyle Style = CellStyle.Normal,
    CellAlignment Alignment = CellAlignment.Left,
    int RowSpan = 1,
    int ColumnSpan = 1,
    bool EmphasizeFirstLine = false)
{
    public static ReportCell Text(string? text, CellStyle style = CellStyle.Normal, CellAlignment alignment = CellAlignment.Left) =>
        new(string.IsNullOrEmpty(text) ? [] : [text], style, alignment);

    public static ReportCell Empty(CellStyle style = CellStyle.Normal) => new([], style);

    public string PlainText => string.Join(" ", Lines);
}

/// <summary>
/// Fila de tabla. Una celda con RowSpan &gt; 1 ocupa también esa columna en las filas siguientes,
/// que entonces tienen una celda menos.
/// </summary>
public sealed record ReportRow(IReadOnlyList<ReportCell> Cells);

/// <summary>
/// Tabla con anchos relativos por columna (por ejemplo 3, 1, 1…). Las filas de encabezado se repiten en cada página.
/// </summary>
public sealed record ReportTable(IReadOnlyList<float> ColumnWidths, IReadOnlyList<ReportRow> HeaderRows, IReadOnlyList<ReportRow> Rows);

/// <summary>Pareja etiqueta-valor del encabezado (ej.: RESPONSABLE: CONSEJO ACADÉMICO).</summary>
public sealed record ReportField(string Label, string Value);

/// <summary>
/// Una sección del documento; cada sección empieza en una página nueva.
/// </summary>
public sealed record ReportSection(
    string Title,
    string? Subtitle,
    IReadOnlyList<ReportField> Fields,
    IReadOnlyList<ReportTable> Tables,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> Paragraphs);

/// <summary>
/// Documento: encabezado institucional y secciones (páginas). Horizontal por defecto, por el ancho de las tablas.
/// </summary>
public sealed record ReportDocument(
    string InstitutionName,
    string FileName,
    IReadOnlyList<ReportSection> Sections,
    bool Landscape = true);

/// <summary>Archivo listo para descargar.</summary>
public sealed record DocumentFile(byte[] Content, string ContentType, string FileName);
