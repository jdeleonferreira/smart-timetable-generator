namespace SmartTimetableGenerator.Domain.Common.Rules;

/// <summary>
/// Reglas compartidas para textos del dominio.
/// </summary>
public static class TextRules
{
    /// <summary>
    /// Valida un texto obligatorio: no vacío y con longitud máxima. Devuelve el texto sin espacios sobrantes.
    /// </summary>
    public static string Required(string? value, int maxLength, string paramName)
    {
        ThrowIfNullOrWhiteSpace(value, paramName);
        var trimmed = value.Trim();
        ThrowIfGreaterThan(trimmed.Length, maxLength, paramName);
        return trimmed;
    }

    /// <summary>
    /// Valida un texto opcional: vacío se convierte en null; si tiene contenido, respeta la longitud máxima.
    /// </summary>
    public static string? Optional(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        ThrowIfGreaterThan(trimmed.Length, maxLength, paramName);
        return trimmed;
    }
}
