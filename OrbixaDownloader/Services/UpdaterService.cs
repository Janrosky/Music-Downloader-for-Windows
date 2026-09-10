using Newtonsoft.Json.Linq;
using OrbixaDownloader.Models;
using System.Diagnostics;
using System.IO.Compression;

namespace OrbixaDownloader.Services;

public class UpdateProgressEventArgs : EventArgs
{
    public string Message { get; set; } = "";
    public int Percent { get; set; }
}

public class UpdaterService
{
    private readonly AppSettings _settings;
    private readonly HttpClient _http;
    private static readonly SemaphoreSlim UpdateLock = new(1, 1);
    public event EventHandler<UpdateProgressEventArgs>? ProgressChanged;

    public UpdaterService(AppSettings settings, HttpClient? http = null)
    {
        _settings = settings;
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("OrbixaDownloader/1.0");
    }

    public async Task CheckAndUpdateAllAsync(CancellationToken ct = default)
    {
        await UpdateLock.WaitAsync(ct);
        try
        {
            var errors = new List<string>();
            foreach (var tool in new[] { "yt-dlp", "ffmpeg", "deno" })
            {
                try { await UpdateToolAsync(tool, ct); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { errors.Add($"{tool}: {ex.Message}"); }
            }
            if (errors.Count > 0)
            {
                string message = "Instalación incompleta. Reintentá en Ajustes → Actualizar ahora.\r\n" + string.Join("\r\n", errors);
                Report(message, 0);
                throw new InvalidOperationException(message);
            }
            Report("Herramientas verificadas y actualizadas.", 100);
        }
        finally { UpdateLock.Release(); }
    }

    internal static bool IsFfmpegAsset(string name) =>
        name.StartsWith("ffmpeg-", StringComparison.OrdinalIgnoreCase) &&
        name.EndsWith("-win64-gpl.zip", StringComparison.OrdinalIgnoreCase);

    private async Task UpdateToolAsync(string tool, CancellationToken ct)
    {
        Report($"Verificando {tool}...", 0);
        string repo = tool switch { "yt-dlp" => "yt-dlp/yt-dlp", "ffmpeg" => "BtbN/FFmpeg-Builds", _ => "denoland/deno" };
        var release = JObject.Parse(await _http.GetStringAsync($"https://api.github.com/repos/{repo}/releases/latest", ct));
        string version = release.Value<string>("tag_name") ?? throw new InvalidDataException("Release sin versión.");
        var asset = release["assets"]?.FirstOrDefault(a => tool switch
        {
            "yt-dlp" => (string?)a["name"] == "yt-dlp.exe",
            "ffmpeg" => IsFfmpegAsset((string?)a["name"] ?? ""),
            _ => (string?)a["name"] == "deno-x86_64-pc-windows-msvc.zip"
        }) ?? throw new InvalidDataException($"No se encontró el paquete Windows de {tool}.");
        string[] destinations = tool switch
        {
            "yt-dlp" => new[] { _settings.YtDlpPath },
            "ffmpeg" => new[] { _settings.FfmpegPath, _settings.FfprobePath },
            _ => new[] { _settings.DenoPath }
        };
        string installed = tool switch { "yt-dlp" => _settings.InstalledYtDlpVersion, "ffmpeg" => _settings.InstalledFfmpegVersion, _ => _settings.InstalledDenoVersion };
        bool valid = true;
        foreach (string path in destinations) valid &= await VerifyExecutableAsync(path, ct);
        if (valid && installed == version) return;
        string staging = Path.Combine(Path.GetDirectoryName(destinations[0])!, ".update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            string package = Path.Combine(staging, tool == "yt-dlp" ? "yt-dlp.exe" : "package.zip");
            Report($"Descargando {tool} {version}...", 10);
            await DownloadFileAsync((string?)asset["browser_download_url"] ?? throw new InvalidDataException("Paquete sin URL."), package, ct);
            string[] sources;
            if (tool == "yt-dlp") sources = new[] { package };
            else sources = await ExtractToolsAsync(package, staging, destinations.Select(Path.GetFileName).Cast<string>().ToArray(), ct);
            foreach (string source in sources)
                if (!await VerifyExecutableAsync(source, ct)) throw new InvalidDataException($"{Path.GetFileName(source)} no pasó la verificación de versión.");
            ct.ThrowIfCancellationRequested();
            InstallFiles(sources, destinations);
            if (tool == "yt-dlp") _settings.InstalledYtDlpVersion = version;
            else if (tool == "ffmpeg") _settings.InstalledFfmpegVersion = version;
            else _settings.InstalledDenoVersion = version;
            _settings.Save();
            Report($"{tool} {version} instalado y verificado.", 95);
        }
        finally { try { Directory.Delete(staging, true); } catch { } }
    }

    internal static async Task<string[]> ExtractToolsAsync(string zipPath, string target, string[] names, CancellationToken ct)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var entries = names.Select(name => zip.Entries.SingleOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException($"El paquete no contiene {name}. Reintentá la actualización.")).ToArray();
        var result = new List<string>();
        foreach (var entry in entries)
        {
            string path = Path.Combine(target, entry.Name);
            await using var input = entry.Open();
            await using var output = File.Create(path);
            await input.CopyToAsync(output, ct);
            result.Add(path);
        }
        return result.ToArray();
    }

    // Each replacement is atomic; backups roll back the pair if either tool is locked.
    internal static void InstallFiles(string[] sources, string[] destinations)
    {
        var backups = new Dictionary<string, string>();
        var installed = new List<string>();
        try
        {
            for (int i = 0; i < sources.Length; i++)
            {
                string destination = destinations[i];
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                if (File.Exists(destination))
                {
                    string backup = destination + ".backup-" + Guid.NewGuid().ToString("N");
                    File.Replace(sources[i], destination, backup);
                    backups.Add(destination, backup);
                }
                else File.Move(sources[i], destination);
                installed.Add(destination);
            }
        }
        catch
        {
            foreach (string destination in installed.AsEnumerable().Reverse())
            {
                if (backups.TryGetValue(destination, out string? backup)) File.Move(backup, destination, true);
                else File.Delete(destination);
            }
            throw;
        }
        finally { foreach (string backup in backups.Values) { try { File.Delete(backup); } catch { } } }
    }

    internal async Task DownloadFileAsync(string url, string destination, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string temporary = destination + ".partial-" + Guid.NewGuid().ToString("N");
        try
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            await using (var input = await response.Content.ReadAsStreamAsync(ct))
            await using (var output = File.Create(temporary))
            {
                await input.CopyToAsync(output, ct);
                if (output.Length == 0 || (response.Content.Headers.ContentLength is long expected && output.Length != expected))
                    throw new InvalidDataException("Descarga incompleta.");
            }
            ct.ThrowIfCancellationRequested();
            File.Move(temporary, destination, true);
        }
        finally { try { File.Delete(temporary); } catch { } }
    }

    internal static async Task<bool> VerifyExecutableAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path)) return false;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            var start = new ProcessStartInfo(path, Path.GetFileName(path).StartsWith("ff", StringComparison.OrdinalIgnoreCase) ? "-version" : "--version")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync(timeout.Token); }
            catch { try { process.Kill(true); } catch { } throw; }
            await stderr;
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(await stdout);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return false; }
        catch (OperationCanceledException) { throw; }
        catch { return false; }
    }

    public string GetInstalledYtDlpVersion() => _settings.InstalledYtDlpVersion;
    private void Report(string message, int percent) => ProgressChanged?.Invoke(this, new UpdateProgressEventArgs { Message = message, Percent = percent });
}
