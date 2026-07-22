using Newtonsoft.Json;
using OrbixaDownloader.Models;
using System.Diagnostics;

namespace OrbixaDownloader.Services
{
    public class VideoMetadata
    {
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Duration { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public string Channel { get; set; } = "";
        public string ViewCount { get; set; } = "";
    }

    public class MetadataService
    {
        private readonly AppSettings _settings;
        private readonly HttpClient _http = new();

        public MetadataService(AppSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Obtiene metadatos del video usando yt-dlp --dump-json (sin descargar)
        /// </summary>
        public async Task<VideoMetadata?> GetMetadataAsync(string url, CancellationToken ct = default)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _settings.YtDlpPath,
                    Arguments = $"--dump-json --no-playlist \"{url}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using var process = Process.Start(psi)!;
                string json = await process.StandardOutput.ReadToEndAsync(ct);
                await process.WaitForExitAsync(ct);

                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(json))
                    return null;

                dynamic data = JsonConvert.DeserializeObject(json)!;

                string title = (string?)data.track ?? (string?)data.title ?? "";
                string artist = (string?)data.artist ?? (string?)data.uploader ?? "";
                string channel = (string?)data.channel ?? (string?)data.uploader ?? "";
                long duration = (long?)data.duration ?? 0;
                long views = (long?)data.view_count ?? 0;

                // Thumbnail de mejor calidad disponible
                string thumbnail = "";
                try
                {
                    // Intentar thumbnails en orden de calidad
                    var thumbList = data.thumbnails;
                    if (thumbList != null)
                    {
                        string? best = null;
                        foreach (var t in thumbList)
                        {
                            best = (string?)t.url;
                        }
                        thumbnail = best ?? "";
                    }
                    if (string.IsNullOrEmpty(thumbnail))
                        thumbnail = (string?)data.thumbnail ?? "";
                }
                catch { thumbnail = (string?)data.thumbnail ?? ""; }

                return new VideoMetadata
                {
                    Title = title,
                    Artist = artist,
                    Channel = channel,
                    Duration = FormatDuration(duration),
                    ThumbnailUrl = thumbnail,
                    ViewCount = FormatViews(views)
                };
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Descarga el thumbnail como Image para mostrarlo en la UI
        /// </summary>
        public async Task<Image?> DownloadThumbnailAsync(string url, int width = 80, int height = 80)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url)) return null;

                byte[] bytes = await _http.GetByteArrayAsync(url);
                using var ms = new MemoryStream(bytes);
                var original = Image.FromStream(ms);
                return new Bitmap(original, new Size(width, height));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Rellena título y artista en el DownloadItem desde los metadatos
        /// </summary>
        public async Task EnrichDownloadItemAsync(DownloadItem item, CancellationToken ct = default)
        {
            var meta = await GetMetadataAsync(item.Url, ct);
            if (meta == null) return;

            if (!string.IsNullOrWhiteSpace(meta.Title)) item.Title = meta.Title;
            if (!string.IsNullOrWhiteSpace(meta.Artist)) item.Artist = meta.Artist;
            if (!string.IsNullOrWhiteSpace(meta.Duration)) item.Duration = meta.Duration;
            if (!string.IsNullOrWhiteSpace(meta.ThumbnailUrl)) item.ThumbnailUrl = meta.ThumbnailUrl;
        }

        private static string FormatDuration(long seconds)
        {
            if (seconds <= 0) return "";
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.Hours > 0
                ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes}:{ts.Seconds:D2}";
        }

        private static string FormatViews(long views)
        {
            if (views >= 1_000_000) return $"{views / 1_000_000.0:F1}M vistas";
            if (views >= 1_000) return $"{views / 1_000.0:F0}K vistas";
            return views > 0 ? $"{views} vistas" : "";
        }
    }
}