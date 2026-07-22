using OrbixaDownloader.Models;

namespace OrbixaDownloader.Forms
{
    /// <summary>
    /// Card visual para cada item en la cola de descargas.
    /// Se auto-refresca cuando el DownloadItem notifica cambios.
    /// </summary>
    public class DownloadCard : Panel
    {
        private readonly DownloadItem _item;
        private readonly Action<DownloadItem> _onCancel;
        private readonly Action<DownloadItem> _onOpenFolder;

        private static readonly Color BgCard = Color.FromArgb(13, 18, 36);
        private static readonly Color BgCard2 = Color.FromArgb(20, 27, 48);
        private static readonly Color Red = Color.FromArgb(219, 41, 85);
        private static readonly Color RedLight = Color.FromArgb(230, 64, 108);
        private static readonly Color Green = Color.FromArgb(34, 197, 94);
        private static readonly Color White = Color.FromArgb(244, 244, 245);
        private static readonly Color Muted = Color.FromArgb(161, 161, 170);
        private static readonly Color Surface = Color.FromArgb(31, 41, 55);

        private System.Windows.Forms.Timer _refreshTimer;

        public DownloadCard(DownloadItem item,
            Action<DownloadItem> onCancel,
            Action<DownloadItem> onOpenFolder)
        {
            _item = item;
            _onCancel = onCancel;
            _onOpenFolder = onOpenFolder;

            Height = 76;
            Margin = new Padding(0, 0, 0, 8);
            BackColor = Color.Transparent;
            DoubleBuffered = true;

            // Subscribe to item changes
            _item.PropertyChanged += (_, _) =>
            {
                if (InvokeRequired) Invoke(Invalidate);
                else Invalidate();
            };

            // Fallback refresh timer (30fps)
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _refreshTimer.Tick += (_, _) =>
            {
                if (_item.Status == DownloadStatus.Downloading ||
                    _item.Status == DownloadStatus.Converting ||
                    _item.Status == DownloadStatus.FetchingInfo)
                    Invalidate();
            };
            _refreshTimer.Start();

            // Click: open folder if completed
            Click += (_, e) =>
            {
                if (_item.Status == DownloadStatus.Completed)
                    _onOpenFolder(_item);
            };
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var r = new Rectangle(0, 0, Width - 2, Height - 4);

            // Card background
            DrawHelper.FillRounded(g, r, 12, BgCard);

            // Status left accent bar
            Color accentColor = _item.Status switch
            {
                DownloadStatus.Downloading => Red,
                DownloadStatus.Converting => Color.FromArgb(234, 179, 8),   // amber
                DownloadStatus.Completed => Green,
                DownloadStatus.Failed => Color.FromArgb(239, 68, 68),   // red error
                DownloadStatus.Cancelled => Surface,
                _ => Color.FromArgb(50, White)
            };
            using var accentBrush = new SolidBrush(accentColor);
            g.FillRectangle(accentBrush, new Rectangle(0, 12, 3, Height - 28));

            // Thumbnail placeholder (left icon)
            var thumbRect = new Rectangle(16, 14, 48, 48);
            DrawHelper.FillRounded(g, thumbRect, 8, Color.FromArgb(30, 41, 62));
            // Music note icon
            using var iconFont = GetFont(20, FontStyle.Regular);
            using var iconBrush = new SolidBrush(Color.FromArgb(60, White));
            g.DrawString("♪", iconFont, iconBrush, thumbRect.X + 12, thumbRect.Y + 10);

            // Title
            using var titleFont = GetFont(10, FontStyle.Bold);
            using var titleBrush = new SolidBrush(White);
            string title = TruncateText(_item.Title, titleFont, g, Width - 200);
            g.DrawString(title, titleFont, titleBrush, 76, 18);

            // Artist + duration
            using var subFont = GetFont(9, FontStyle.Regular);
            using var subBrush = new SolidBrush(Muted);
            string sub = _item.Artist;
            if (!string.IsNullOrEmpty(_item.Duration)) sub += $"  •  {_item.Duration}";
            g.DrawString(sub, subFont, subBrush, 76, 36);

            // Progress bar (only when active)
            if (_item.Status == DownloadStatus.Downloading ||
                _item.Status == DownloadStatus.Converting)
            {
                var trackRect = new Rectangle(76, 54, Width - 180, 4);
                DrawHelper.FillRounded(g, trackRect, 2, Surface);

                int fillW = (int)(trackRect.Width * _item.Progress / 100.0);
                if (fillW > 2)
                {
                    using var fillBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(trackRect.X, trackRect.Y, Math.Max(fillW, 2), trackRect.Height),
                        Red, RedLight, 0f);
                    DrawHelper.FillRounded(g, new Rectangle(trackRect.X, trackRect.Y, fillW, trackRect.Height), 2, fillBrush);
                }
            }

            // Status badge (right side)
            string badge = _item.StatusText;
            Color badgeColor = _item.Status switch
            {
                DownloadStatus.Completed => Color.FromArgb(30, 34, 197, 94),
                DownloadStatus.Failed => Color.FromArgb(30, 239, 68, 68),
                DownloadStatus.Cancelled => Color.FromArgb(20, White),
                _ => Color.FromArgb(20, Red)
            };
            Color badgeText = _item.Status switch
            {
                DownloadStatus.Completed => Green,
                DownloadStatus.Failed => Color.FromArgb(239, 68, 68),
                _ => Muted
            };

            using var badgeFont = GetFont(8, FontStyle.Regular);
            var badgeSize = g.MeasureString(badge, badgeFont);
            var badgeRect = new Rectangle(
                Width - (int)badgeSize.Width - 28,
                (Height - 22) / 2,
                (int)badgeSize.Width + 16, 22);
            DrawHelper.FillRounded(g, badgeRect, 6, badgeColor);
            using var badgeBrush = new SolidBrush(badgeText);
            g.DrawString(badge, badgeFont, badgeBrush,
                badgeRect.X + 8,
                badgeRect.Y + (badgeRect.Height - badgeSize.Height) / 2);

            // Cancel X (top-right, only when active)
            if (_item.Status == DownloadStatus.Pending ||
                _item.Status == DownloadStatus.Downloading ||
                _item.Status == DownloadStatus.FetchingInfo)
            {
                using var xFont = GetFont(9, FontStyle.Regular);
                using var xBrush = new SolidBrush(Color.FromArgb(80, White));
                g.DrawString("✕", xFont, xBrush, Width - 22, 8);
            }

            // Hover open-folder hint
            if (_item.Status == DownloadStatus.Completed)
            {
                using var hintFont = GetFont(8, FontStyle.Regular);
                using var hintBrush = new SolidBrush(Color.FromArgb(50, White));
                g.DrawString("📂 Abrir carpeta", hintFont, hintBrush, 76, 56);
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            // Cancel button (top-right 20x20 area)
            if (e.X > Width - 30 && e.Y < 28)
                _onCancel(_item);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _refreshTimer?.Dispose();
            base.Dispose(disposing);
        }

        private static string TruncateText(string text, Font font, Graphics g, int maxWidth)
        {
            if (g.MeasureString(text, font).Width <= maxWidth) return text;
            while (text.Length > 3 && g.MeasureString(text + "…", font).Width > maxWidth)
                text = text[..^1];
            return text + "…";
        }

        private static Font GetFont(float size, FontStyle style) =>
            MainForm.GetFont(size, style);
    }
}