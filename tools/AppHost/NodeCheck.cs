using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AppHost;

/// <summary>
/// Comprueba que haya un Node.js utilizable antes de registrar el cliente web, para fallar con un
/// mensaje claro en vez de un error críptico de npm (p. ej. un Node de otro perfil de Windows).
/// </summary>
internal static partial class NodeCheck
{
    public static bool IsAvailable(int minimumMajor, out string problem)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("node", "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null || !process.WaitForExit(TimeSpan.FromSeconds(10)))
            {
                problem = "no se pudo ejecutar 'node'. Instale Node.js " + minimumMajor + " o superior (ver src/WebClient/.nvmrc).";
                return false;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            var match = VersionPattern().Match(output);
            if (process.ExitCode != 0 || !match.Success)
            {
                var error = process.StandardError.ReadToEnd().Trim().Split('\n')[0];
                problem = $"'node' falló al ejecutarse ({error}). Revise que el 'node' del PATH sea accesible para este usuario.";
                return false;
            }

            var major = int.Parse(match.Groups[1].Value);
            if (major < minimumMajor)
            {
                problem = $"se encontró Node.js {output} y se requiere {minimumMajor} o superior (ver src/WebClient/.nvmrc).";
                return false;
            }

            problem = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            problem = "no se encontró 'node' en el PATH. Instale Node.js " + minimumMajor + " o superior (ver src/WebClient/.nvmrc).";
            return false;
        }
    }

    [GeneratedRegex(@"^v(\d+)\.")]
    private static partial Regex VersionPattern();
}
