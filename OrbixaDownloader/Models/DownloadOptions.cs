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

        public DownloadOptions Snapshot() => (DownloadOptions)MemberwiseClone();

        public IEnumerable<string> GetArguments(string url)
        {
            var args = new List<string> { "--no-playlist", "--progress", "--newline", "-o", Path.Combine(OutputFolder, "%(title)s.%(ext)s") };
            if (Type == DownloadType.AudioOnly)
            {
                args.AddRange(new[] { "-x", "--audio-format", AudioFormat.ToString().ToLowerInvariant(), "--audio-quality", ((int)Quality).ToString() });
                if (EmbedThumbnail && AudioFormat is AudioFormat.MP3 or AudioFormat.M4A or AudioFormat.FLAC or AudioFormat.OGG)
                    args.Add("--embed-thumbnail");
            }
            else
            {
                string format = VideoFormat.ToString().ToLowerInvariant();
                string selection = VideoFormat switch
                {
                    VideoFormat.MP4 => "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best",
                    VideoFormat.WEBM => "bestvideo[ext=webm]+bestaudio[ext=webm]/best[ext=webm]/best",
                    _ => "bestvideo+bestaudio/best"
                };
                args.AddRange(new[] { "-f", selection, "--merge-output-format", format, "--recode-video", format });
            }
            if (EmbedMetadata) args.Add("--embed-metadata");
            args.Add("--"); args.Add(url);
            return args;
        }
    }
}
