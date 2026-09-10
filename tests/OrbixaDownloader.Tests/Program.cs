using OrbixaDownloader.Models;
using OrbixaDownloader.Services;
using OrbixaDownloader.Forms;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text;

internal static class Program
{
    private static string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
    private static string Work = Path.Combine(Root, "tests", "artifacts");
    [STAThread]
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        try
        {
            if (args.Contains("--fake-error")) { Console.Error.WriteLine("ERROR: conversión inválida, canción ñ." + new string('x', 12000)); return 1; }
            if (args.Contains("--fake-empty")) return 1;
            if (args.Contains("--fake-wait")) { Thread.Sleep(30000); return 0; }
            Directory.CreateDirectory(Work);
            if (args.Contains("--install")) { Install().GetAwaiter().GetResult(); return 0; }
            if (args.Contains("--integration")) { Integration().GetAwaiter().GetResult(); return 0; }
            Tests().GetAwaiter().GetResult();
            Render();
            Console.WriteLine("ALL TESTS PASSED"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    static void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL " + name); Console.WriteLine("PASS " + name); }
    static AppSettings Settings() => new()
    {
        YtDlpPath = Path.Combine(Root, "OrbixaDownloader/bin/Debug/net8.0-windows/bin/yt-dlp.exe"),
        FfmpegPath = Path.Combine(Root, "OrbixaDownloader/bin/Debug/net8.0-windows/bin/ffmpeg.exe"),
        DenoPath = Path.Combine(Root, "OrbixaDownloader/bin/Debug/net8.0-windows/bin/deno.exe"),
        DefaultOutputFolder = Path.Combine(Work, "downloads"), MaxConcurrentDownloads = 1
    };
    static async Task Install()
    {
        var updater = new UpdaterService(Settings());
        updater.ProgressChanged += (_, e) => Console.WriteLine(e.Message);
        await updater.CheckAndUpdateAllAsync();
    }
    static async Task Tests()
    {
        var newPresets = new[] { ThemePreset.CoralNocturno, ThemePreset.TurquesaNocturno, ThemePreset.CieloArtico, ThemePreset.GrafitoSuave, ThemePreset.AmbarNocturno };
        var newPalettes = newPresets.Select(preset => OrbixaThemes.Resolve(preset)).ToArray();
        Check(newPalettes.Select(palette => palette.Canvas.ToArgb()).Distinct().Count() == newPresets.Length, "New presets produce distinct palettes");
        Check(newPalettes.All(palette => palette.HasReadableContrast() && palette.HasReadableContrastForLargeText()), "New presets WCAG contrast");
        Check(newPalettes.All(palette => palette.HasReadableInteractionContrast()), "Hover transition preserves readable text");
        Check(Enum.GetValues<ThemePreset>().Where(p => p is not ThemePreset.SystemHighContrast and not ThemePreset.Personalizado).All(preset => OrbixaThemes.IsValid(preset)), "Theme palettes contrast");
        Check(OrbixaThemes.IsValid(ThemePreset.CieloCobalto) && OrbixaThemes.IsValid(ThemePreset.OlivaLimon) && OrbixaThemes.IsValid(ThemePreset.AmapolaAzul) && OrbixaThemes.IsValid(ThemePreset.ArcillaCielo), "New theme presets interaction contrast");
        Check(new[] { ThemePreset.ObsidianaAmbar, ThemePreset.MarfilGrafito, ThemePreset.BosqueCobre, ThemePreset.AzulTintaMandarina, ThemePreset.PizarraLima }.All(preset => OrbixaThemes.IsValid(preset)), "Approved theme presets contrast");
        Check(OrbixaThemes.Resolve(ThemePreset.ObsidianaAmbar).Success != Color.Empty && OrbixaThemes.Resolve(ThemePreset.PizarraLima).Error != Color.Empty, "Semantic state roles");
        var customTheme = new CustomThemeSettings { Canvas = CuratedColor.Cielo, Primary = CuratedColor.Coral, Focus = CuratedColor.Cielo };
        Check(OrbixaThemes.IsValid(ThemePreset.Personalizado, customTheme), "Custom curated palette contrast");
        string customJson = Newtonsoft.Json.JsonConvert.SerializeObject(new AppSettings { Theme = ThemePreset.Personalizado, CustomTheme = customTheme });
        var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(customJson)!;
        restored.Normalize();
        Check(restored.Theme == ThemePreset.Personalizado && restored.CustomTheme.Primary == CuratedColor.Coral, "Custom palette round trip");
        var invalidPreferences = new AppSettings { Theme = ThemePreset.Personalizado, CustomTheme = new CustomThemeSettings { Surface = (CuratedColor)999 } };
        invalidPreferences.Normalize();
        Check(invalidPreferences.Theme == ThemePreset.OrbixaMineral && invalidPreferences.Language == UiLanguage.Espanol, "Invalid custom palette fallback");
        var preferences = new AppSettings { Theme = (ThemePreset)999, Language = (UiLanguage)999 };
        preferences.Normalize();
        Check(preferences.Theme == ThemePreset.OrbixaMineral && preferences.Language == UiLanguage.Espanol, "Invalid preferences fallback");
        UiText.Language = UiLanguage.English;
        Check(UiText.StatusText(DownloadStatus.Completed) == "Completed", "English status localization");
        Check(UiText.Localize("Lista de reproducción") == "Playlist" && UiText.Localize("⚙  Ajustes") == "⚙  Settings", "English UI localization");
        Check(UiText.Get("Obsidiana Ámbar", "Amber Obsidian") == "Amber Obsidian" && UiText.Get("Bosque Cobre", "Copper Forest") == "Copper Forest", "Theme names localization");
        Check(UiText.LocalizeProgress("Verificando yt-dlp...") == "Verifying yt-dlp...", "English progress localization");
        Check(UiText.Localize("Asegurate de tener WMP instalado") == "Asegurate de tener WMP instalado", "Technical details remain raw");
        UiText.Language = UiLanguage.Espanol;
        Check(UpdaterService.IsFfmpegAsset("ffmpeg-master-latest-win64-gpl.zip"), "CA1 real BtbN asset");
        Check(!UpdaterService.IsFfmpegAsset("ffmpeg-master-latest-win64-gpl-shared.zip") && !UpdaterService.IsFfmpegAsset("ffmpeg-master-latest-linux64-gpl.zip"), "CA1 excludes shared/linux");
        string temp = Path.Combine(Work, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
        string zip = Path.Combine(temp, "pair.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            foreach (var name in new[] { "ffmpeg.exe", "ffprobe.exe" }) { using var writer = new StreamWriter(archive.CreateEntry("release/bin/" + name).Open()); writer.Write("binary"); }
        var paths = await UpdaterService.ExtractToolsAsync(zip, temp, new[] { "ffmpeg.exe", "ffprobe.exe" }, default);
        Check(paths.All(File.Exists), "CA1 both extracted");
        bool incomplete = false;
        try { await UpdaterService.ExtractToolsAsync(zip, temp, new[] { "missing.exe" }, default); } catch (InvalidDataException) { incomplete = true; }
        Check(incomplete, "CA1 incomplete archive fails");
        string destination = Path.Combine(temp, "existing.exe"); File.WriteAllText(destination, "previous");
        var broken = new UpdaterService(Settings(), new HttpClient(new BrokenHandler()));
        bool failed = false;
        try { await broken.DownloadFileAsync("https://example.test/binary", destination, default); } catch (InvalidDataException) { failed = true; }
        Check(failed && File.ReadAllText(destination) == "previous" && !Directory.GetFiles(temp, "*.partial-*").Any(), "CA2 failed transfer preserves binary and cleans stage");
        var settings = Settings();
        var downloader = new DownloadService(settings);
        Check(DownloadService.IsSupportedUrl(" https://youtu.be/I067BonnW48 ") && !DownloadService.IsSupportedUrl("httpbad") && !DownloadService.IsSupportedUrl("file:///test") && !DownloadService.IsSupportedUrl(""), "CA7 URL validation");
        bool missing = false;
        try { new DownloadService(new AppSettings { YtDlpPath = Path.Combine(temp, "absent.exe") }).ValidatePreflight(new DownloadOptions { OutputFolder = temp }); } catch (InvalidOperationException ex) { missing = ex.Message.Contains("Actualizar"); }
        Check(missing, "CA3 missing tools recovery");
        bool unwritable = false;
        try { DownloadService.ValidateOutputFolder(destination); } catch (InvalidOperationException) { unwritable = true; }
        Check(unwritable, "CA3 invalid output rejected");
        foreach (var format in Enum.GetValues<VideoFormat>())
        {
            var opts = new DownloadOptions { Type = DownloadType.VideoAndAudio, VideoFormat = format, OutputFolder = temp };
            var arguments = downloader.CreateStartInfo("https://example.test/media", opts).ArgumentList;
            Check(arguments.Contains(format.ToString().ToLowerInvariant()) && arguments.Contains("--recode-video") && arguments.Contains("deno:" + settings.DenoPath) && arguments[^1] == "https://example.test/media", "CA8 arguments " + format);
        }
        var audio = new DownloadOptions { OutputFolder = temp, Quality = AudioQuality.Low, AudioFormat = AudioFormat.WAV };
        Check(audio.GetArguments("https://example.test").Contains("9") && !audio.GetArguments("https://example.test").Contains("--embed-thumbnail"), "CA8 quality and thumbnail compatibility");
        // Real child process fixtures: copy apphost with its runtime alongside it; URL serves as fixture mode.
        string fixture = Path.Combine(temp, "fixture.exe");
        File.Copy(Environment.ProcessPath!, fixture);
        foreach (var file in Directory.GetFiles(AppContext.BaseDirectory)) if (Path.GetExtension(file) is ".json" or ".dll") File.Copy(file, Path.Combine(temp, Path.GetFileName(file)), true);
        settings.YtDlpPath = fixture;
        settings.FfmpegPath = destination;
        File.WriteAllText(settings.FfprobePath, "fixture");
        settings.DenoPath = destination;
        downloader = new DownloadService(settings);
        foreach (string mode in new[] { "--fake-error", "--fake-empty" })
        {
            var item = new DownloadItem { Url = mode, CancellationSource = new() };
            await downloader.DownloadAsync(item, audio);
            if (!item.ErrorDetails.Contains("código 1")) Console.WriteLine(item.ErrorDetails);
            Check(item.Status == DownloadStatus.Failed && item.ErrorDetails.Contains("código 1") && (mode != "--fake-error" || (item.ErrorDetails.Contains("canción ñ") && item.ErrorDetails.Length > 12000)), "CA4 UTF8 stderr/fallback " + mode);
        }
        var active = new DownloadItem { Url = "--fake-wait", CancellationSource = new() };
        var queued = new DownloadItem { Url = "--fake-wait", CancellationSource = new() };
        Task first = downloader.DownloadAsync(active, audio), second = downloader.DownloadAsync(queued, audio);
        queued.CancellationSource.Cancel(); await second;
        active.CancellationSource.Cancel(); await first;
        Check(active.Status == DownloadStatus.Cancelled && queued.Status == DownloadStatus.Cancelled, "CA5 active/queued cancellation");
        var next = new DownloadItem { Url = "--fake-empty", CancellationSource = new() };
        await downloader.DownloadAsync(next, audio).WaitAsync(TimeSpan.FromSeconds(10));
        Check(next.Status == DownloadStatus.Failed, "CA5 semaphore reusable");
    }
    static async Task Integration()
    {
        var settings = Settings();
        foreach (string path in new[] { settings.YtDlpPath, settings.FfmpegPath, settings.FfprobePath, settings.DenoPath })
            Check(await UpdaterService.VerifyExecutableAsync(path, default), "CA2 version " + Path.GetFileName(path));
        string source = Path.Combine(Work, "synthetic.mkv");
        await Run(settings.FfmpegPath, "-y", "-f", "lavfi", "-i", "testsrc=size=160x90:rate=10", "-f", "lavfi", "-i", "sine=frequency=440:sample_rate=44100", "-t", "2", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-c:a", "aac", source);
        using var listener = new HttpListener(); listener.Prefixes.Add("http://localhost:18765/"); listener.Start();
        using var serverCt = new CancellationTokenSource();
        Task server = Task.Run(async () => { while (!serverCt.IsCancellationRequested) { try { var context = await listener.GetContextAsync(); byte[] bytes = File.ReadAllBytes(source); context.Response.ContentType = "video/x-matroska"; context.Response.ContentLength64 = bytes.Length; await context.Response.OutputStream.WriteAsync(bytes); context.Response.Close(); } catch when (serverCt.IsCancellationRequested) { break; } } });
        try
        {
            foreach (string format in new[] { "MP3", "WEBM", "MKV" })
            {
                var opts = new DownloadOptions { OutputFolder = Path.Combine(Work, "integration", format), EmbedMetadata = false, EmbedThumbnail = false };
                if (format != "MP3") { opts.Type = DownloadType.VideoAndAudio; opts.VideoFormat = Enum.Parse<VideoFormat>(format); }
                var item = new DownloadItem { Url = "http://localhost:18765/synthetic.mkv", CancellationSource = new() };
                await new DownloadService(settings).DownloadAsync(item, opts);
                Check(item.Status == DownloadStatus.Completed && File.Exists(item.OutputPath), "CA6/8 synthetic " + format + " " + item.ErrorDetails);
                await Run(settings.FfprobePath, "-v", "error", "-show_entries", "format=format_name,duration", "-of", "json", item.OutputPath);
            }
            var userItem = new DownloadItem { Url = "https://youtu.be/I067BonnW48", CancellationSource = new() };
            await new DownloadService(settings).DownloadAsync(userItem, new DownloadOptions { OutputFolder = Path.Combine(Work, "user-mp3"), AudioFormat = AudioFormat.MP3 });
            Check(userItem.Status == DownloadStatus.Completed, "User URL MP3 " + userItem.ErrorDetails);
            Console.WriteLine("MP3_PATH=" + userItem.OutputPath);
        }
        finally { serverCt.Cancel(); listener.Stop(); await server; }
    }
    static async Task Run(string exe, params string[] args)
    {
        var psi = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string arg in args) psi.ArgumentList.Add(arg);
        using var p = Process.Start(psi)!; var stdout = p.StandardOutput.ReadToEndAsync(); var stderr = p.StandardError.ReadToEndAsync(); await p.WaitForExitAsync();
        Console.WriteLine(await stdout);
        if (p.ExitCode != 0) throw new Exception(await stderr); await stderr;
    }
    static void Render()
    {
        Application.EnableVisualStyles();
        using var main = new MainForm(Settings());
        main.Show(); Application.DoEvents();
        foreach (Size size in new[] { new Size(820, 560), new Size(980, 660) })
        {
            main.Size = size; main.PerformLayout(); Application.DoEvents();
            using var bitmap = new Bitmap(main.Width, main.Height); main.DrawToBitmap(bitmap, main.ClientRectangle);
            bitmap.Save(Path.Combine(Work, $"main-{size.Width}.png"));
        }
        using var settings = new SettingsForm(Settings()); settings.Show(); Application.DoEvents();
        using var image = new Bitmap(settings.Width, settings.Height); settings.DrawToBitmap(image, settings.ClientRectangle); image.Save(Path.Combine(Work, "settings.png"));
        Console.WriteLine("PASS render main/settings");
    }
    sealed class BrokenHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var content = new ByteArrayContent(new byte[] { 1, 2 }); content.Headers.ContentLength = 10;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
}



