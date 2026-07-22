using Newtonsoft.Json;
using OrbixaDownloader.Models;
using System.Diagnostics;
using System.IO.Compression;

namespace OrbixaDownloader.Services
{
    public class UpdateProgressEventArgs : EventArgs
    {
        public string Message { get; set; } = "";
        public int Percent { get; set; }
    }

    public class UpdaterService
    {
        private readonly AppSettings _settings;
        private readonly HttpClient _http;

        private const string YtDlpReleasesUrl =
            "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";
        private const string FfmpegReleasesUrl =
            "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest";

        public event EventHandler<UpdateProgressEventArgs>? ProgressChanged;

        public UpdaterService(AppSettings settings)
        {
            _settings = settings;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("OrbixaDownloader/1.0");
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task CheckAndUpdateAllAsync(CancellationToken ct = default)
        {
            _settings.EnsureDirectoriesExist();

            Report("Verificando yt-dlp...", 0);
            await CheckAndUpdateYtDlpAsync(ct);

            Report("Verificando ffmpeg...", 50);
            await CheckAndUpdateFfmpegAsync(ct);

            Report("Todo actualizado.", 100);
        }

        // ─── yt-dlp ───────────────────────────────────────────────────────────

        private async Task CheckAndUpdateYtDlpAsync(CancellationToken ct)
        {
            try
            {
                string json = await _http.GetStringAsync(YtDlpReleasesUrl, ct);
                dynamic release = JsonConvert.DeserializeObject(json)!;
                string latestVersion = (string)release.tag_name;

                bool needsUpdate = !File.Exists(_settings.YtDlpPath)
                    || _settings.InstalledYtDlpVersion != latestVersion;

                if (!needsUpdate)
                {
                    Report($"yt-dlp ya está actualizado ({latestVersion})", 25);
                    return;
                }

                Report($"Descargando yt-dlp {latestVersion}...", 10);

                // Buscar el asset yt-dlp.exe en los releases
                string? downloadUrl = null;
                foreach (var asset in release.assets)
                {
                    string name = (string)asset.name;
                    if (name == "yt-dlp.exe")
                    {
                        downloadUrl = (string)asset.browser_download_url;
                        break;
                    }
                }

                if (downloadUrl == null) return;

                await DownloadFileAsync(downloadUrl, _settings.YtDlpPath, ct);

                _settings.InstalledYtDlpVersion = latestVersion;
                _settings.Save();

                Report($"yt-dlp {latestVersion} instalado.", 40);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Report($"No se pudo actualizar yt-dlp: {ex.Message}", 40);
            }
        }

        // ─── ffmpeg ───────────────────────────────────────────────────────────

        private async Task CheckAndUpdateFfmpegAsync(CancellationToken ct)
        {
            try
            {
                // Si ya existe ffmpeg y no hay versión nueva disponible fácil de comparar,
                // solo lo descargamos si no existe en disco.
                if (File.Exists(_settings.FfmpegPath))
                {
                    Report("ffmpeg ya está instalado.", 75);
                    return;
                }

                Report("Descargando ffmpeg (primera instalación)...", 55);

                string json = await _http.GetStringAsync(FfmpegReleasesUrl, ct);
                dynamic release = JsonConvert.DeserializeObject(json)!;

                // Buscamos el build essentials win64
                string? downloadUrl = null;
                foreach (var asset in release.assets)
                {
                    string name = (string)asset.name;
                    if (name.Contains("win64") && name.Contains("essentials") && name.EndsWith(".zip"))
                    {
                        downloadUrl = (string)asset.browser_download_url;
                        break;
                    }
                }

                if (downloadUrl == null)
                {
                    Report("No se encontró build de ffmpeg para Windows.", 90);
                    return;
                }

                string zipPath = Path.Combine(Path.GetTempPath(), "ffmpeg_temp.zip");
                await DownloadFileAsync(downloadUrl, zipPath, ct);

                Report("Extrayendo ffmpeg...", 85);
                await ExtractFfmpegAsync(zipPath, ct);

                Report("ffmpeg instalado.", 95);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Report($"No se pudo instalar ffmpeg: {ex.Message}", 95);
            }
        }

        private async Task ExtractFfmpegAsync(string zipPath, CancellationToken ct)
        {
            string extractDir = Path.Combine(Path.GetTempPath(), "ffmpeg_extract");

            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, true);

            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, extractDir), ct);

            // ffmpeg.exe está dentro de una subcarpeta /bin/
            string? ffmpegExe = Directory
                .GetFiles(extractDir, "ffmpeg.exe", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (ffmpegExe != null)
                File.Copy(ffmpegExe, _settings.FfmpegPath, overwrite: true);

            // Limpiar temporales
            try
            {
                File.Delete(zipPath);
                Directory.Delete(extractDir, true);
            }
            catch { /* No crítico */ }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private async Task DownloadFileAsync(string url, string destPath, CancellationToken ct)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            long total = response.Content.Headers.ContentLength ?? 0;
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var file = File.Create(destPath);

            var buffer = new byte[81920];
            long downloaded = 0;
            int read;

            while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), ct);
                downloaded += read;

                if (total > 0)
                {
                    int pct = (int)(downloaded * 100 / total);
                    Report($"Descargando... {pct}%", pct);
                }
            }
        }

        public string GetInstalledYtDlpVersion()
        {
            try
            {
                if (!File.Exists(_settings.YtDlpPath)) return "No instalado";

                var psi = new ProcessStartInfo(_settings.YtDlpPath, "--version")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi)!;
                string ver = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return ver;
            }
            catch { return "Desconocido"; }
        }

        private void Report(string message, int percent) =>
            ProgressChanged?.Invoke(this, new UpdateProgressEventArgs
            {
                Message = message,
                Percent = percent
            });
    }
}