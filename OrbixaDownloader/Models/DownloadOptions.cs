namespace OrbixaDownloader.Models
{
    public enum AudioFormat
    {
        MP3,
        M4A,
        FLAC,
        WAV,
        OGG
    }

    public enum VideoFormat
    {
        MP4,
        WEBM,
        MKV
    }

    public enum DownloadType
    {
        AudioOnly,
        VideoAndAudio
    }

    public enum AudioQuality
    {
        Best = 0,
        High = 2,
        Medium = 5,
        Low = 9
    }

    public class DownloadOptions
    {
        public DownloadType Type { get; set; } = DownloadType.AudioOnly;
        public AudioFormat AudioFormat { get; set; } = AudioFormat.MP3;
        public VideoFormat VideoFormat { get; set; } = VideoFormat.MP4;
        public AudioQuality Quality { get; set; } = AudioQuality.Best;
        public string OutputFolder { get; set; } = "";
        public bool EmbedThumbnail { get; set; } = true;
        public bool EmbedMetadata { get; set; } = true;

        public string GetYtDlpAudioArgs(string url, string outputFolder)
        {
            string ext = AudioFormat.ToString().ToLower();
            string quality = ((int)Quality).ToString();
            string output = Path.Combine(outputFolder, "%(title)s.%(ext)s");

            var args = new List<string>
            {
                $"\"{url}\"",
                "-x",
                $"--audio-format {ext}",
                $"--audio-quality {quality}",
                $"-o \"{output}\"",
                "--no-playlist",
                "--progress",
                "--newline"
            };

            if (EmbedThumbnail) args.Add("--embed-thumbnail");
            if (EmbedMetadata) args.Add("--embed-metadata");

            return string.Join(" ", args);
        }

        public string GetYtDlpVideoArgs(string url, string outputFolder)
        {
            string output = Path.Combine(outputFolder, "%(title)s.%(ext)s");

            return string.Join(" ", new[]
            {
                $"\"{url}\"",
                "-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\"",
                "--merge-output-format mp4",
                $"-o \"{output}\"",
                "--no-playlist",
                "--progress",
                "--newline",
                EmbedMetadata ? "--embed-metadata" : ""
            });
        }
    }
}