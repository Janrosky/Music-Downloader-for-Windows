using Newtonsoft.Json;

namespace OrbixaDownloader.Models
{
    public class AppSettings
    {
        private static readonly string SettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        // Rutas de dependencias
        public string YtDlpPath { get; set; } = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "bin", "yt-dlp.exe");

        public string FfmpegPath { get; set; } = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "bin", "ffmpeg.exe");

        [JsonIgnore] public string FfprobePath => Path.Combine(Path.GetDirectoryName(FfmpegPath)!, "ffprobe.exe");
        public string DenoPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "deno.exe");
        public string InstalledDenoVersion { get; set; } = "";

        // Preferencias del usuario
        public string DefaultOutputFolder { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "OrbixaDownloader");

        public DownloadType DefaultDownloadType { get; set; } = DownloadType.AudioOnly;
        public AudioFormat DefaultAudioFormat { get; set; } = AudioFormat.MP3;
        public VideoFormat DefaultVideoFormat { get; set; } = VideoFormat.MP4;
        public AudioQuality DefaultQuality { get; set; } = AudioQuality.Best;
        public bool EmbedThumbnail { get; set; } = true;
        public bool EmbedMetadata { get; set; } = true;
        public int MaxConcurrentDownloads { get; set; } = 2;
        public bool CheckUpdatesOnStartup { get; set; } = true;

        public ThemePreset Theme { get; set; } = ThemePreset.OrbixaMineral;
        public CustomThemeSettings CustomTheme { get; set; } = new();
        public UiLanguage Language { get; set; } = UiLanguage.Espanol;

        // Versiones instaladas (para comparar con GitHub)
        public string InstalledYtDlpVersion { get; set; } = "";
        public string InstalledFfmpegVersion { get; set; } = "";

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                    settings.Normalize();
                    return settings;
                }
            }
            catch { /* Si falla, usamos defaults */ }

            var defaults = new AppSettings();
            defaults.Normalize();
            return defaults;
        }

        public void Normalize()
        {
            if (CustomTheme == null) CustomTheme = new CustomThemeSettings();
            if (!Enum.IsDefined(Theme) || !OrbixaThemes.IsValid(Theme, CustomTheme)) Theme = ThemePreset.OrbixaMineral;
            if (!Enum.IsDefined(Language)) Language = UiLanguage.Espanol;
            OrbixaDownloader.Forms.UiText.Language = Language;
        }

        public void Save()
        {
            try
            {
                Normalize();
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch { /* Silencioso */ }
        }

        public void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(YtDlpPath)!);
            Directory.CreateDirectory(DefaultOutputFolder);
        }
    }
}
