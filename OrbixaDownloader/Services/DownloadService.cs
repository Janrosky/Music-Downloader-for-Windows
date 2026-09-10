using OrbixaDownloader.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OrbixaDownloader.Services;

public class DownloadService
{
    private readonly AppSettings _settings;
    private readonly SemaphoreSlim _semaphore;
    private static readonly Regex ProgressRegex = new(@"\[download\]\s+(\d+\.?\d*)%", RegexOptions.Compiled);
    internal const string OutputMarker = "ORBIXA_FILE:";
    public DownloadService(AppSettings settings)
    {
        _settings = settings;
        _semaphore = new SemaphoreSlim(Math.Clamp(settings.MaxConcurrentDownloads, 1, 4));
    }

    public static bool IsSupportedUrl(string value) => Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) && !string.IsNullOrWhiteSpace(uri.Host);

    public void ValidatePreflight(DownloadOptions options)
    {
        var missing = new[] { _settings.YtDlpPath, _settings.FfmpegPath, _settings.FfprobePath, _settings.DenoPath }
            .Where(path => !File.Exists(path) || new FileInfo(path).Length == 0).Select(Path.GetFileName).ToArray();
        if (missing.Length > 0) throw new InvalidOperationException("Faltan herramientas: " + string.Join(", ", missing) + ". Abrí Ajustes → Actualizar ahora y volvé a intentar.");
        ValidateOutputFolder(options.OutputFolder);
    }

    public static void ValidateOutputFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new InvalidOperationException("Elegí una carpeta de destino en Ajustes.");
        try
        {
            Directory.CreateDirectory(folder);
            string probe = Path.Combine(folder, ".orbixa-write-" + Guid.NewGuid().ToString("N"));
            using var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
            stream.WriteByte(0);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { throw new InvalidOperationException("No se puede escribir en la carpeta de destino. Elegí otra carpeta en Ajustes.", ex); }
    }

    internal ProcessStartInfo CreateStartInfo(string url, DownloadOptions options)
    {
        var psi = new ProcessStartInfo(_settings.YtDlpPath)
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            CreateNoWindow = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        foreach (string arg in options.GetArguments(url).SkipLast(2)) psi.ArgumentList.Add(arg);
        AddRuntimeArguments(psi, _settings);
        psi.ArgumentList.Add("--print");
        psi.ArgumentList.Add("after_move:" + OutputMarker + "%(filepath)j");
        psi.ArgumentList.Add("--no-simulate");
        psi.ArgumentList.Add("--"); psi.ArgumentList.Add(url);
        return psi;
    }

    internal static void AddRuntimeArguments(ProcessStartInfo psi, AppSettings settings)
    {
        psi.ArgumentList.Add("--ignore-config");
        psi.ArgumentList.Add("--encoding"); psi.ArgumentList.Add("utf-8");
        psi.ArgumentList.Add("--ffmpeg-location"); psi.ArgumentList.Add(Path.GetDirectoryName(settings.FfmpegPath)!);
        psi.ArgumentList.Add("--js-runtimes"); psi.ArgumentList.Add("deno:" + settings.DenoPath);
    }

    public async Task DownloadAsync(DownloadItem item, DownloadOptions options)
    {
        bool acquired = false;
        var ct = (item.CancellationSource ??= new CancellationTokenSource()).Token;
        try
        {
            await _semaphore.WaitAsync(ct);
            acquired = true;
            ct.ThrowIfCancellationRequested();
            ValidatePreflight(options);
            item.ErrorDetails = "";
            item.OutputPath = "";
            item.Status = DownloadStatus.FetchingInfo;
            item.Progress = 0;
            using var process = new Process { StartInfo = CreateStartInfo(item.Url, options) };
            var errors = new StringBuilder();
            string outputPath = "";
            process.Start();
            item.Status = DownloadStatus.Downloading;
            async Task ReadOutputAsync()
            {
                while (await process.StandardOutput.ReadLineAsync() is string line)
                {
                    if (line.StartsWith(OutputMarker, StringComparison.Ordinal))
                    {
                        try { outputPath = JsonSerializer.Deserialize<string>(line[OutputMarker.Length..]) ?? ""; }
                        catch (JsonException) { }
                    }
                    else ParseOutputLine(line, item);
                }
            }
            async Task ReadErrorsAsync()
            {
                while (await process.StandardError.ReadLineAsync() is string line)
                {
                    // One writer; retained in memory only, never persisted or sent elsewhere.
                    errors.AppendLine(line);
                }
            }
            Task stdout = ReadOutputAsync(), stderr = ReadErrorsAsync();
            try { await process.WaitForExitAsync(ct); }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
                await process.WaitForExitAsync();
                throw;
            }
            finally { await Task.WhenAll(stdout, stderr); }
            ct.ThrowIfCancellationRequested();
            if (process.ExitCode != 0)
            {
                string cause = errors.Length > 0 ? errors.ToString().Trim() : "yt-dlp no informó detalles del error. Verificá el enlace y actualizá las herramientas en Ajustes.";
                Fail(item, $"Error de descarga (código {process.ExitCode}).\r\n\r\n{cause}");
            }
            else if (string.IsNullOrWhiteSpace(outputPath) || !File.Exists(outputPath))
                Fail(item, "yt-dlp finalizó, pero no se pudo verificar el archivo de salida. Revisá la carpeta de destino y volvé a intentar.");
            else
            {
                item.OutputPath = Path.GetFullPath(outputPath);
                item.Title = Path.GetFileNameWithoutExtension(outputPath);
                item.Progress = 100;
                item.Status = DownloadStatus.Completed;
            }
        }
        catch (OperationCanceledException) { item.Status = DownloadStatus.Cancelled; }
        catch (Exception ex) { Fail(item, ex.Message); }
        finally { if (acquired) _semaphore.Release(); }
    }

    private static void Fail(DownloadItem item, string details)
    {
        item.ErrorDetails = details;
        item.Status = DownloadStatus.Failed;
    }

    private static void ParseOutputLine(string line, DownloadItem item)
    {
        var progress = ProgressRegex.Match(line);
        if (progress.Success && double.TryParse(progress.Groups[1].Value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double pct))
        {
            item.Progress = pct;
            item.Status = DownloadStatus.Downloading;
        }
        else if (line.Contains("[ExtractAudio]") || line.Contains("[Merger]") || line.Contains("[VideoConvertor]"))
            item.Status = DownloadStatus.Converting;
        else if (line.Contains("Destination:"))
            item.Title = Path.GetFileNameWithoutExtension(line.Split("Destination:").Last().Trim());
    }
}

