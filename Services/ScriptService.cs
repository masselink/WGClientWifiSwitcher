using System;
using System.Diagnostics;
using System.IO;

namespace MasselGUARD.Services
{
    /// <summary>
    /// Executes pre/post tunnel scripts.
    /// Supports file paths (.bat / .ps1) and @embed:... inline scripts.
    /// Returns (exitCode, stdout+stderr). No UI references.
    /// </summary>
    public class ScriptService
    {
        private const string EmbedPrefix = "@embed:";

        public record ScriptResult(int ExitCode, string Output);

        public ScriptResult Run(string scriptValue, string hookName, string tunnelName)
        {
            if (string.IsNullOrWhiteSpace(scriptValue))
                return new(0, "");

            string? tempFile = null;
            string  path;
            string  ext;

            // Resolve embedded scripts to a temp file
            if (scriptValue.StartsWith(EmbedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string content = scriptValue[EmbedPrefix.Length..];
                ext     = content.TrimStart().StartsWith("#!") ? ".ps1" : ".bat";
                tempFile = Path.Combine(Path.GetTempPath(),
                    $"masselguard_{hookName}_{tunnelName}{ext}");
                File.WriteAllText(tempFile, content, System.Text.Encoding.UTF8);
                path = tempFile;
            }
            else
            {
                path = scriptValue.Trim();
                ext  = Path.GetExtension(path).ToLowerInvariant();
            }

            try
            {
                var psi = ext == ".ps1"
                    ? new ProcessStartInfo("powershell.exe",
                        $"-ExecutionPolicy Bypass -NonInteractive -File \"{path}\"")
                    : new ProcessStartInfo("cmd.exe", $"/c \"{path}\"");

                psi.UseShellExecute        = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError  = true;
                psi.CreateNoWindow         = true;

                using var proc = Process.Start(psi)!;
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                string combined = (stdout + stderr).Trim();
                return new(proc.ExitCode, combined);
            }
            catch (Exception ex)
            {
                return new(-1, ex.Message);
            }
            finally
            {
                if (tempFile != null && File.Exists(tempFile))
                    try { File.Delete(tempFile); } catch { }
            }
        }
    }
}
