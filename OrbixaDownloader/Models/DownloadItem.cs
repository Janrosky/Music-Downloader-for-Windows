using System.ComponentModel;

namespace OrbixaDownloader.Models
{
    public enum DownloadStatus
    {
        Pending,
        FetchingInfo,
        Downloading,
        Converting,
        Completed,
        Failed,
        Cancelled
    }

    public class DownloadItem : INotifyPropertyChanged
    {
        private string _title = "Obteniendo información...";
        private string _artist = "";
        private string _duration = "";
        private double _progress;
        private DownloadStatus _status = DownloadStatus.Pending;
        private string _statusText = "En cola";
        private string _thumbnailUrl = "";
        private string _outputPath = "";

        public string ErrorDetails { get; set; } = "";
        public DownloadOptions? Options { get; set; }

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Url { get; set; } = "";
        public DateTime AddedAt { get; set; } = DateTime.Now;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        public string Artist
        {
            get => _artist;
            set { _artist = value; OnPropertyChanged(nameof(Artist)); }
        }

        public string Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(nameof(Duration)); }
        }

        public double Progress
        {
            get => _progress;
            set { _progress = Math.Clamp(value, 0, 100); OnPropertyChanged(nameof(Progress)); }
        }

        public DownloadStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                StatusText = value switch
                {
                    DownloadStatus.Pending => "En cola",
                    DownloadStatus.FetchingInfo => "Obteniendo info...",
                    DownloadStatus.Downloading => $"Descargando {Progress:F0}%",
                    DownloadStatus.Converting => "Convirtiendo...",
                    DownloadStatus.Completed => "Completado",
                    DownloadStatus.Failed => "Error",
                    DownloadStatus.Cancelled => "Cancelado",
                    _ => ""
                };
                OnPropertyChanged(nameof(Status));
            }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(nameof(StatusText)); }
        }

        public string ThumbnailUrl
        {
            get => _thumbnailUrl;
            set { _thumbnailUrl = value; OnPropertyChanged(nameof(ThumbnailUrl)); }
        }

        public string OutputPath
        {
            get => _outputPath;
            set { _outputPath = value; OnPropertyChanged(nameof(OutputPath)); }
        }

        public CancellationTokenSource? CancellationSource { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
