using OrbixaDownloader.Services;
using OrbixaDownloader.Models;

namespace OrbixaDownloader.Forms
{
    public partial class SplashForm : Form
    {
        private readonly AppSettings _settings;
        private readonly UpdaterService _updater;

        private const int W = 520;
        private const int H = 320;

        private static readonly Color BgDeep = Color.FromArgb(5, 8, 22);
        private static readonly Color Red = Color.FromArgb(219, 41, 85);
        private static readonly Color RedLight = Color.FromArgb(230, 64, 108);
        private static readonly Color White = Color.FromArgb(244, 244, 245);
        private static readonly Color Muted = Color.FromArgb(161, 161, 170);
        private static readonly Color Surface = Color.FromArgb(31, 41, 55);

        private int _progress = 0;
        private string _statusMsg = "Iniciando...";
        private System.Windows.Forms.Timer? _pulseTimer;
        private float _pulseAlpha = 0f;
        private bool _pulseUp = true;

        public SplashForm(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            _updater = new UpdaterService(settings);
            _updater.ProgressChanged += OnUpdateProgress;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            _pulseTimer = new System.Windows.Forms.Timer { Interval = 30 };
            _pulseTimer.Tick += (_, _) =>
            {
                _pulseAlpha += _pulseUp ? 0.04f : -0.04f;
                if (_pulseAlpha >= 1f) { _pulseAlpha = 1f; _pulseUp = false; }
                if (_pulseAlpha <= 0f) { _pulseAlpha = 0f; _pulseUp = true; }
                Invalidate();
            };
            _pulseTimer.Start();

            Task.Run(RunStartupAsync);
        }

        private async Task RunStartupAsync()
        {
            try
            {
                if (_settings.CheckUpdatesOnStartup)
                    await _updater.CheckAndUpdateAllAsync();
                else
                {
                    for (int i = 0; i <= 100; i += 20)
                    {
                        await Task.Delay(100);
                        int captured = i;
                        Invoke(() => { _progress = captured; _statusMsg = "Cargando..."; Invalidate(); });
                    }
                }
            }
            catch (Exception ex)
            {
                Invoke(() => { _statusMsg = $"Advertencia: {ex.Message}"; Invalidate(); });
                await Task.Delay(1500);
            }
            finally
            {
                // Solo cerrar el splash — Program.cs abre MainForm
                Invoke(() =>
                {
                    _pulseTimer?.Stop();
                    _pulseTimer?.Dispose();
                    Close();
                });
            }
        }

        private void OnUpdateProgress(object? sender, UpdateProgressEventArgs args)
        {
            if (!IsHandleCreated || IsDisposed) return;
            try
            {
                Invoke(() =>
                {
                    _progress = args.Percent;
                    _statusMsg = args.Message;
                    Invalidate();
                });
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var bgBrush = new SolidBrush(BgDeep);
            g.FillRectangle(bgBrush, ClientRectangle);

            DrawGlow(g, W / 2, -20, 280, Color.FromArgb(40, 219, 41, 85));

            var card = new Rectangle(40, 40, W - 80, H - 80);
            DrawRoundedRect(g, card, 18,
                Color.FromArgb(180, 13, 18, 36),
                Color.FromArgb(40, 219, 41, 85), 1f);

            int lx = W / 2, ly = 105;
            using var logoFont = GetFont(32, FontStyle.Bold);
            var logoSize = g.MeasureString("ORBIXA", logoFont);
            using var logoBrush = new SolidBrush(White);
            g.DrawString("ORBIXA", logoFont, logoBrush,
                lx - logoSize.Width / 2, ly - logoSize.Height / 2);

            int dotAlpha = (int)(80 + 175 * _pulseAlpha);
            using var dotBrush = new SolidBrush(Color.FromArgb(dotAlpha, Red));
            g.FillEllipse(dotBrush, lx - logoSize.Width / 2 - 2, ly + 14, 8, 8);

            using var subFont = GetFont(11, FontStyle.Regular);
            using var subBrush = new SolidBrush(Muted);
            string sub = "Music Downloader";
            var subSize = g.MeasureString(sub, subFont);
            g.DrawString(sub, subFont, subBrush,
                lx - subSize.Width / 2, ly + logoSize.Height / 2 + 4);

            int divY = ly + 60;
            using var divPen = new Pen(Color.FromArgb(30, 255, 255, 255), 1f);
            g.DrawLine(divPen, card.Left + 32, divY, card.Right - 32, divY);

            using var statFont = GetFont(10, FontStyle.Regular);
            using var statBrush = new SolidBrush(Muted);
            var statSize = g.MeasureString(_statusMsg, statFont);
            g.DrawString(_statusMsg, statFont, statBrush,
                lx - statSize.Width / 2, divY + 18);

            int barX = card.Left + 32, barY = divY + 44;
            int barW = card.Width - 64, barH = 4;
            DrawRoundedRect(g, new Rectangle(barX, barY, barW, barH), 2, Surface, Color.Empty, 0f);

            int fillW = (int)(barW * _progress / 100.0);
            if (fillW > 2)
            {
                using var fillBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(barX, barY, Math.Max(fillW, 2), barH),
                    Red, RedLight, 0f);
                FillRoundedRect(g, new Rectangle(barX, barY, fillW, barH), 2, fillBrush);
            }

            using var pctFont = GetFont(9, FontStyle.Regular);
            using var pctBrush = new SolidBrush(Color.FromArgb(100, White));
            string pctText = $"{_progress}%";
            var pctSize = g.MeasureString(pctText, pctFont);
            g.DrawString(pctText, pctFont, pctBrush,
                barX + barW - pctSize.Width, barY + barH + 6);

            using var verFont = GetFont(8, FontStyle.Regular);
            using var verBrush = new SolidBrush(Color.FromArgb(60, White));
            string ver = "v1.0.0";
            var verSize = g.MeasureString(ver, verFont);
            g.DrawString(ver, verFont, verBrush,
                lx - verSize.Width / 2, card.Bottom - 22);
        }

        private static void DrawGlow(Graphics g, int cx, int cy, int radius, Color color)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
            using var brush = new System.Drawing.Drawing2D.PathGradientBrush(path)
            {
                CenterColor = color,
                SurroundColors = new[] { Color.Transparent }
            };
            g.FillPath(brush, path);
        }

        private static void DrawRoundedRect(Graphics g, Rectangle r, int radius,
            Color fill, Color stroke, float strokeWidth)
        {
            using var path = GetRoundedPath(r, radius);
            if (fill != Color.Empty)
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
            if (stroke != Color.Empty && strokeWidth > 0)
                using (var p = new Pen(stroke, strokeWidth)) g.DrawPath(p, path);
        }

        private static void FillRoundedRect(Graphics g, Rectangle r, int radius, Brush brush)
        {
            using var path = GetRoundedPath(r, radius);
            g.FillPath(brush, path);
        }

        private static System.Drawing.Drawing2D.GraphicsPath GetRoundedPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Font GetFont(float size, FontStyle style)
        {
            try { return new Font("Space Grotesk", size, style, GraphicsUnit.Point); }
            catch { return new Font("Segoe UI", size, style, GraphicsUnit.Point); }
        }
    }
}