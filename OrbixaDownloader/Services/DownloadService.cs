using OrbixaDownloader.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace OrbixaDownloader.Services
{
    public class DownloadService
    {
        private readonly AppSettings _settings;
        private readonly SemaphoreSlim _semaphore;

        // Regex para parsear el output de yt-dlp
        // Ejemplo: [download]  45.2% of  3.54MiB at  1.23MiB/s ETA 00:02
        private static readonly Regex ProgressRegex =
            new(@"\[download\]\s+(\d+\.?\d*)%", RegexOptions.Compiled);

        private static readonly Regex TitleRegex =
            new(@"\[(?:youtube|info)\].*?:\s+(.+)", RegexOptions.Compiled);

        public DownloadService(AppSettings settings)
        {
            _settings = settings;
            _semaphore = new SemaphoreSlim(settings.MaxConcurrentDownloads);
        }

        public async Task DownloadAsync(DownloadItem item, DownloadOptions options)
        {
            await _semaphore.WaitAsync(item.CancellationSource!.Token);

            try
            {
                item.Status = DownloadStatus.FetchingInfo;
                item.Progress = 0;

                string args = options.Type == DownloadType.AudioOnly
                    ? options.GetYtDlpAudioArgs(item.Url, options.OutputFolder)
                    : options.GetYtDlpVideoArgs(item.Url, options.OutputFolder);

                var psi = new ProcessStartInfo
                {
                    FileName = _settings.YtDlpPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                // Inyectar ffmpeg si existe
                if (File.Exists(_settings.FfmpegPath))
                {
                    string ffmpegDir = Path.GetDirectoryName(_settings.FfmpegPath)!;
                    psi.Arguments += $" --ffmpeg-location \"{ffmpegDir}\"";
                }

                using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                var outputLines = new List<string>();

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data == null) return;
                    ParseOutputLine(e.Data, item);
                    outputLines.Add(e.Data);
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null) outputLines.Add($"ERR: {e.Data}");
                };

                item.Status = DownloadStatus.Downloading;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Esperar con soporte de cancelación
                var ct = item.CancellationSource.Token;
                await Task.Run(() =>
                {
                    while (!process.HasExited)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            try { process.Kill(); } catch { }
                            break;
                        }
                        Thread.Sleep(100);
                    }
                }, ct);

                if (ct.IsCancellationRequested)
                {
                    item.Status = DownloadStatus.Cancelled;
                    return;
                }

                if (process.ExitCode == 0)
                {
                    item.Progress = 100;
                    item.Status = DownloadStatus.Completed;
                    item.OutputPath = ResolveOutputPath(item.Title, options);
                }
                else
                {
                    item.Status = DownloadStatus.Failed;
                    item.StatusText = $"Error (código {process.ExitCode})";
                }
            }
            catch (OperationCanceledException)
            {
                item.Status = DownloadStatus.Cancelled;
            }
            catch (Exception ex)
            {
                item.Status = DownloadStatus.Failed;
                item.StatusText = $"Error: {ex.Message}";
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void ParseOutputLine(string line, DownloadItem item)
        {
            // Progreso porcentual
            var progressMatch = ProgressRegex.Match(line);
            if (progressMatch.Success)
            {
                if (double.TryParse(progressMatch.Groups[1].Value,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double pct))
                {
                    item.Progress = pct;
                    item.Status = DownloadStatus.Downloading;
                    item.StatusText = $"Descargando {pct:F0}%";
                }
                return;
            }

            // Detectar conversión
            if (line.Contains("[ExtractAudio]") || line.Contains("[Merger]") ||
                line.Contains("Deleting original"))
            {
                item.Status = DownloadStatus.Converting;
                return;
            }

            // Extraer título desde el output de yt-dlp
            if (line.StartsWith("[youtube]") && line.Contains(": ") && item.Title == "Obteniendo información...")
            {
                var parts = line.Split(": ", 2);
                if (parts.Length > 1 && !parts[1].StartsWith("Downloading"))
                    item.Title = parts[1].Trim();
            }

            // Título desde la descarga
            if (line.Contains("Destination:"))
            {
                string filename = Path.GetFileNameWithoutExtension(
                    line.Split("Destination:").Last().Trim());
                if (!string.IsNullOrWhiteSpace(filename))
                    item.Title = filename;
            }
        }

        private string ResolveOutputPath(string title, DownloadOptions options)
        {
            string ext = options.Type == DownloadType.AudioOnly
                ? options.AudioFormat.ToString().ToLower()
                : "mp4";

            // Sanitizar nombre de archivo
            string safe = string.Concat(title.Select(c =>
                Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

            return Path.Combine(options.OutputFolder, $"{safe}.{ext}");
        }

        public async Task<string> GetVideoTitleAsync(string url)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _settings.YtDlpPath,
                    Arguments = $"--get-title --no-playlist \"{url}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi)!;
                string title = await p.StandardOutput.ReadToEndAsync();
                await p.WaitForExitAsync();
                return title.Trim();
            }
            catch
            {
                return "";
            }
        }
    }
}