using System.Text;

namespace SmartTimetableGenerator.Infrastructure.Identity;

/// <summary>
/// Configuración de los tokens (sección <c>Jwt</c>). La llave de firma debe tener al menos 32 caracteres
/// y guardarse fuera del código en producción (secretos de usuario, variables de entorno o un almacén de secretos).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = "SmartTimetableGenerator";

    public string Audience { get; set; } = "SmartTimetableGenerator";

    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Duración del token. Por defecto, una jornada (8 horas).</summary>
    public int ExpirationMinutes { get; set; } = 480;

    public bool HasValidKey => Encoding.UTF8.GetByteCount(SigningKey) >= MinimumKeyBytes;
}
