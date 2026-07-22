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
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { /* Si falla, usamos defaults */ }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
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