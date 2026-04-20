using Endpoint.Models;
using System.Diagnostics;

namespace Endpoint.Services
{
    public class PowerShellService
    {
        private readonly ILogger<PowerShellService> _logger;

        public PowerShellService(ILogger<PowerShellService> logger)
        {
            _logger = logger;
        }

        public Task<(string output, string error, int exitCode)> ExecuteAsync(string script, int timeoutSeconds = 30)
            => ExecuteAsync(script, timeoutSeconds, false);

        public async Task<(string output, string error, int exitCode)> ExecuteAsync(
            string script, int timeoutSeconds, bool requiresAdmin)
        {
            _logger.LogInformation("Executing PowerShell script (timeout: {Timeout}s, admin: {Admin})",
                timeoutSeconds, requiresAdmin);

            if (requiresAdmin)
                return await ExecuteElevatedAsync(script, timeoutSeconds);

            // Non-admin: run hidden with redirected output
            var tempFile = Path.Combine(Path.GetTempPath(), $"ep_{Guid.NewGuid():N}.ps1");
            await File.WriteAllTextAsync(tempFile, script);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{tempFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cts.Token);

                var output = await outputTask;
                var error = await errorTask;

                _logger.LogInformation("PowerShell completed with exit code {ExitCode}", process.ExitCode);
                return (output.Trim(), error.Trim(), process.ExitCode);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(true); } catch { }
                _logger.LogWarning("PowerShell script timed out after {Timeout}s", timeoutSeconds);
                return ("", $"Script timed out after {timeoutSeconds} seconds", -1);
            }
            finally
            {
                try { File.Delete(tempFile); } catch { }
            }
        }

        private async Task<(string output, string error, int exitCode)> ExecuteElevatedAsync(
            string script, int timeoutSeconds)
        {
            // For admin actions: UAC elevation prompt + visible PowerShell window
            // Since UseShellExecute=true can't redirect stdout, capture output via temp files
            var guid = Guid.NewGuid().ToString("N");
            var scriptFile = Path.Combine(Path.GetTempPath(), $"ep_{guid}.ps1");
            var outputFile = Path.Combine(Path.GetTempPath(), $"ep_out_{guid}.txt");
            var errorFile = Path.Combine(Path.GetTempPath(), $"ep_err_{guid}.txt");
            var exitCodeFile = Path.Combine(Path.GetTempPath(), $"ep_exit_{guid}.txt");

            // Wrapper script: run the real script, capture output/errors to temp files
            var wrapperScript = $@"
try {{
    $result = & '{scriptFile.Replace("'", "''")}' 2>&1 | Out-String
    $result | Out-File -FilePath '{outputFile.Replace("'", "''")}' -Encoding UTF8 -Force
    Write-Host $result
    '0' | Out-File -FilePath '{exitCodeFile.Replace("'", "''")}' -Encoding UTF8 -Force
}} catch {{
    $_.Exception.Message | Out-File -FilePath '{errorFile.Replace("'", "''")}' -Encoding UTF8 -Force
    Write-Host ('ERROR: ' + $_.Exception.Message) -ForegroundColor Red
    '1' | Out-File -FilePath '{exitCodeFile.Replace("'", "''")}' -Encoding UTF8 -Force
}}
";
            await File.WriteAllTextAsync(scriptFile, wrapperScript + "\n# Original script:\n" + script);

            // Rewrite scriptFile to be the wrapper that calls itself
            var wrapperFile = Path.Combine(Path.GetTempPath(), $"ep_wrap_{guid}.ps1");
            await File.WriteAllTextAsync(scriptFile, script);
            await File.WriteAllTextAsync(wrapperFile, wrapperScript);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{wrapperFile}\"",
                UseShellExecute = true,
                Verb = "runas"  // Triggers UAC elevation prompt
            };

            try
            {
                using var process = new Process { StartInfo = psi };
                process.Start();

                // Wait for the elevated process to finish
                // Add 30s extra to account for UAC prompt time
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds + 30));
                try { await process.WaitForExitAsync(cts.Token); }
                catch (OperationCanceledException)
                {
                    try { process.Kill(true); } catch { }
                    return ("", $"Script timed out after {timeoutSeconds} seconds", -1);
                }

                // Read results from temp files
                var output = File.Exists(outputFile) ? (await File.ReadAllTextAsync(outputFile)).Trim() : "";
                var error = File.Exists(errorFile) ? (await File.ReadAllTextAsync(errorFile)).Trim() : "";
                var exitCode = 0;
                if (File.Exists(exitCodeFile))
                {
                    int.TryParse((await File.ReadAllTextAsync(exitCodeFile)).Trim(), out exitCode);
                }

                _logger.LogInformation("Elevated PowerShell completed. Exit code: {ExitCode}", exitCode);
                return (output, error, exitCode);
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // User cancelled the UAC prompt
                _logger.LogWarning("UAC elevation was cancelled by user");
                return ("", "Elevation cancelled by user", -1);
            }
            finally
            {
                try { File.Delete(scriptFile); } catch { }
                try { File.Delete(wrapperFile); } catch { }
                try { File.Delete(outputFile); } catch { }
                try { File.Delete(errorFile); } catch { }
                try { File.Delete(exitCodeFile); } catch { }
            }
        }
    }
}
