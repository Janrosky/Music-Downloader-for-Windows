using OrbixaDownloader.Models;
using OrbixaDownloader.Services;

namespace OrbixaDownloader.Forms
{
    public partial class MainForm : Form
    {
        // ── Colors ────────────────────────────────────────────────────────────
        internal static readonly Color BgDeep = Color.FromArgb(5, 8, 22);
        internal static readonly Color BgCard = Color.FromArgb(13, 18, 36);
        internal static readonly Color Red = Color.FromArgb(219, 41, 85);
        internal static readonly Color RedLight = Color.FromArgb(230, 64, 108);
        internal static readonly Color White = Color.FromArgb(244, 244, 245);
        internal static readonly Color Muted = Color.FromArgb(161, 161, 170);
        internal static readonly Color Surface = Color.FromArgb(31, 41, 55);
        internal static readonly Color Surface2 = Color.FromArgb(20, 27, 45);

        // ── Services ──────────────────────────────────────────────────────────
        private readonly AppSettings _settings;
        private readonly DownloadService _downloader;
        private readonly MetadataService _metadata;
        private readonly List<DownloadItem> _queue = new();

        // ── UI refs ───────────────────────────────────────────────────────────
        private Panel _sidebar = null!;
        private Panel _topBar = null!;
        private Panel _contentArea = null!;
        private TextBox _urlBox = null!;
        private ComboBox _formatCombo = null!;
        private ComboBox _typeCombo = null!;
        private Button _downloadBtn = null!;
        private Button _pasteBtn = null!;
        private FlowLayoutPanel _queuePanel = null!;
        private Label _queueTitle = null!;
        private Label _statsLabel = null!;

        // ── Tooltip ───────────────────────────────────────────────────────────
        private readonly ToolTip _toolTip = new ToolTip();

        // ── Drag ──────────────────────────────────────────────────────────────
        private bool _dragging;
        private Point _dragStart;

        public MainForm(AppSettings settings)
        {
            _settings = settings;
            _downloader = new DownloadService(settings);
            _metadata = new MetadataService(settings);
            InitializeComponent();
            BuildUI();
        }

        // ── Build UI ──────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // ── Sidebar ───────────────────────────────────────────────────────
            _sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(8, 11, 28),
                Padding = new Padding(0)
            };
            _sidebar.Paint += PaintSidebar;

            var logoLabel = new Label
            {
                Text = "ORBIXA",
                Font = GetFont(18, FontStyle.Bold),
                ForeColor = White,
                AutoSize = false,
                Width = 220,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 24
            };
            _sidebar.Controls.Add(logoLabel);

            string[] navItems = { "⬇  Descargar", "📋  Cola", "⚙  Ajustes" };
            int navY = 110;
            foreach (var item in navItems)
            {
                var btn = new Label
                {
                    Text = item,
                    Font = GetFont(10, FontStyle.Regular),
                    ForeColor = item.StartsWith("⬇") ? White : Muted,
                    BackColor = item.StartsWith("⬇") ? Color.FromArgb(35, 219, 41, 85) : Color.Transparent,
                    AutoSize = false,
                    Width = 180,
                    Height = 38,
                    Left = 20,
                    Top = navY,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(16, 0, 0, 0),
                    Cursor = Cursors.Hand
                };

                // Navegación entre secciones
                if (item.StartsWith("⚙"))
                    btn.Click += (_, _) =>
                    {
                        using var sf = new SettingsForm(_settings);
                        sf.ShowDialog(this);
                    };

                DrawHelper.AddRoundedAppearance(btn, 8);
                _sidebar.Controls.Add(btn);
                navY += 48;
            }

            var verLabel = new Label
            {
                Text = "v1.0.0",
                Font = GetFont(8, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, White),
                AutoSize = false,
                Width = 220,
                Height = 24,
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _sidebar.Controls.Add(verLabel);

            // ── Top bar ───────────────────────────────────────────────────────
            _topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(10, 14, 32),
            };
            _topBar.Paint += PaintTopBar;
            _topBar.MouseDown += (_, e) => { _dragging = true; _dragStart = e.Location; };
            _topBar.MouseMove += (_, e) => { if (_dragging) Location = new Point(Left + e.X - _dragStart.X, Top + e.Y - _dragStart.Y); };
            _topBar.MouseUp += (_, _) => _dragging = false;

            var closeBtn = MakeWindowBtn("✕", Color.FromArgb(219, 41, 85));
            var minBtn = MakeWindowBtn("─", Surface);
            var maxBtn = MakeWindowBtn("□", Surface);
            closeBtn.Click += (_, _) => Application.Exit();
            minBtn.Click += (_, _) => WindowState = FormWindowState.Minimized;
            maxBtn.Click += (_, _) => WindowState = WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal : FormWindowState.Maximized;
            closeBtn.Top = minBtn.Top = maxBtn.Top = 11;
            closeBtn.Anchor = minBtn.Anchor = maxBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _topBar.Controls.AddRange(new Control[] { closeBtn, minBtn, maxBtn });
            _topBar.Resize += (_, _) =>
            {
                closeBtn.Left = _topBar.Width - 40;
                maxBtn.Left = _topBar.Width - 80;
                minBtn.Left = _topBar.Width - 120;
            };

            // Botón abrir reproductor
            var playerBtn = new Button
            {
                Text = "♪  Reproductor",
                Font = GetFont(9, FontStyle.Bold),
                ForeColor = White,
                BackColor = Color.FromArgb(40, 219, 41, 85),
                FlatStyle = FlatStyle.Flat,
                Width = 130,
                Height = 30,
                Top = 11,
                Left = 240,
                Cursor = Cursors.Hand
            };
            playerBtn.FlatAppearance.BorderSize = 0;
            playerBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(219, 41, 85);
            DrawHelper.MakeRounded(playerBtn, 8);
            playerBtn.Click += (_, _) =>
            {
                var player = new PlayerForm(
                    _queue.Where(x => x.Status == DownloadStatus.Completed && File.Exists(x.OutputPath))
                          .Select(x => x.OutputPath));
                player.Show();
            };
            _topBar.Controls.Add(playerBtn);

            _statsLabel = new Label
            {
                Text = "0 descargas",
                Font = GetFont(9, FontStyle.Regular),
                ForeColor = Muted,
                AutoSize = true,
                Top = 18,
                Left = 240
            };
            _topBar.Controls.Add(_statsLabel);

            // ── Content area ──────────────────────────────────────────────────
            _contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDeep,
                Padding = new Padding(32, 28, 32, 24)
            };

            var addTitle = MakeLabel("Nueva descarga", 14, FontStyle.Bold, White);
            addTitle.Top = 0; addTitle.Left = 0;

            var addSub = MakeLabel("Pegá un link de YouTube, playlist o SoundCloud", 10, FontStyle.Regular, Muted);
            addSub.Top = 28; addSub.Left = 0;

            // URL row
            var urlPanel = new Panel
            {
                Left = 0,
                Top = 56,
                Width = _contentArea.Width - 64,
                Height = 48,
                BackColor = Surface2,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            DrawHelper.MakeRounded(urlPanel, 12);

            _urlBox = new TextBox
            {
                PlaceholderText = "https://youtube.com/watch?v=...",
                Font = GetFont(10, FontStyle.Regular),
                ForeColor = White,
                BackColor = Surface2,
                BorderStyle = BorderStyle.None,
                Left = 16,
                Top = 14,
                Width = urlPanel.Width - 60,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            _pasteBtn = MakeIconButton("⎘", urlPanel.Width - 44, 6);
            _pasteBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            _pasteBtn.Click += (_, _) =>
            {
                _urlBox.Text = Clipboard.GetText();
                _urlBox.Focus();
            };
            // ← Forma correcta para Button estándar
            _toolTip.SetToolTip(_pasteBtn, "Pegar del portapapeles");

            urlPanel.Controls.AddRange(new Control[] { _urlBox, _pasteBtn });

            // Options row
            var optPanel = new Panel { Left = 0, Top = 116, Width = 600, Height = 44 };

            var typeLabel = MakeLabel("Tipo:", 9, FontStyle.Regular, Muted);
            typeLabel.Left = 0; typeLabel.Top = 12;

            _typeCombo = MakeCombo(new[] { "Solo audio", "Video + Audio" }, 52, 0);
            _typeCombo.SelectedIndex = 0;
            _typeCombo.SelectedIndexChanged += (_, _) => UpdateFormatCombo();

            var fmtLabel = MakeLabel("Formato:", 9, FontStyle.Regular, Muted);
            fmtLabel.Left = 200; fmtLabel.Top = 12;

            _formatCombo = MakeCombo(new[] { "MP3", "M4A", "FLAC", "WAV" }, 252, 0);
            _formatCombo.SelectedIndex = 0;

            optPanel.Controls.AddRange(new Control[] { typeLabel, _typeCombo, fmtLabel, _formatCombo });

            // Download button
            _downloadBtn = new Button
            {
                Text = "  ⬇  Descargar",
                Font = GetFont(11, FontStyle.Bold),
                ForeColor = White,
                BackColor = Red,
                FlatStyle = FlatStyle.Flat,
                Left = 0,
                Top = 176,
                Width = 200,
                Height = 46,
                Cursor = Cursors.Hand
            };
            _downloadBtn.FlatAppearance.BorderSize = 0;
            DrawHelper.MakeRounded(_downloadBtn, 12);
            _downloadBtn.Click += OnDownloadClicked;
            _downloadBtn.MouseEnter += (_, _) => _downloadBtn.BackColor = RedLight;
            _downloadBtn.MouseLeave += (_, _) => _downloadBtn.BackColor = Red;

            // Divider
            var divider = new Panel
            {
                Left = 0,
                Top = 240,
                Height = 1,
                BackColor = Color.FromArgb(25, 255, 255, 255),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            _queueTitle = MakeLabel("Cola de descargas", 12, FontStyle.Bold, White);
            _queueTitle.Top = 256; _queueTitle.Left = 0;

            _queuePanel = new FlowLayoutPanel
            {
                Left = 0,
                Top = 286,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
            };

            _contentArea.Controls.AddRange(new Control[]
            {
                addTitle, addSub, urlPanel, optPanel,
                _downloadBtn, divider, _queueTitle, _queuePanel
            });

            _contentArea.Resize += (_, _) =>
            {
                urlPanel.Width = _contentArea.Width - 64;
                _urlBox.Width = urlPanel.Width - 60;
                _pasteBtn.Left = urlPanel.Width - 44;
                divider.Width = _contentArea.Width - 64;
                _queuePanel.Width = _contentArea.Width - 64;
                _queuePanel.Height = _contentArea.Height - 310;
            };

            Controls.Add(_contentArea);
            Controls.Add(_topBar);
            Controls.Add(_sidebar);
        }

        // ── Download logic ────────────────────────────────────────────────────
        private async void OnDownloadClicked(object? sender, EventArgs e)
        {
            string url = _urlBox.Text.Trim();
            if (string.IsNullOrEmpty(url)) return;
            if (!url.StartsWith("http")) { ShowToast("URL inválida"); return; }

            _urlBox.Text = "";
            _downloadBtn.Enabled = false;
            _downloadBtn.Text = "  ⏳  Obteniendo info...";

            var item = new DownloadItem
            {
                Url = url,
                CancellationSource = new CancellationTokenSource()
            };

            var options = BuildOptions();

            _queue.Add(item);
            var card = new DownloadCard(item, OnCancelItem, OnOpenFolder);
            card.Width = _queuePanel.Width - 12;
            _queuePanel.Controls.Add(card);
            UpdateStats();

            // Fetch metadata
            _ = Task.Run(() => _metadata.EnrichDownloadItemAsync(item, item.CancellationSource.Token));

            // Start download
            _ = Task.Run(() => _downloader.DownloadAsync(item, options))
                .ContinueWith(_ => Invoke(() =>
                {
                    _downloadBtn.Enabled = true;
                    _downloadBtn.Text = "  ⬇  Descargar";
                    UpdateStats();
                }));

            _downloadBtn.Enabled = true;
            _downloadBtn.Text = "  ⬇  Descargar";
        }

        private DownloadOptions BuildOptions()
        {
            bool isAudio = _typeCombo.SelectedIndex == 0;
            var opts = new DownloadOptions
            {
                Type = isAudio ? DownloadType.AudioOnly : DownloadType.VideoAndAudio,
                OutputFolder = _settings.DefaultOutputFolder,
                EmbedThumbnail = _settings.EmbedThumbnail,
                EmbedMetadata = _settings.EmbedMetadata
            };

            if (isAudio)
            {
                opts.AudioFormat = _formatCombo.SelectedItem?.ToString() switch
                {
                    "M4A" => AudioFormat.M4A,
                    "FLAC" => AudioFormat.FLAC,
                    "WAV" => AudioFormat.WAV,
                    _ => AudioFormat.MP3
                };
            }
            return opts;
        }

        private void OnCancelItem(DownloadItem item) =>
            item.CancellationSource?.Cancel();

        private void OnOpenFolder(DownloadItem item)
        {
            string path = string.IsNullOrEmpty(item.OutputPath)
                ? _settings.DefaultOutputFolder
                : Path.GetDirectoryName(item.OutputPath) ?? _settings.DefaultOutputFolder;

            if (Directory.Exists(path))
                System.Diagnostics.Process.Start("explorer.exe", path);
        }

        private void UpdateFormatCombo()
        {
            _formatCombo.Items.Clear();
            if (_typeCombo.SelectedIndex == 0)
                _formatCombo.Items.AddRange(new object[] { "MP3", "M4A", "FLAC", "WAV" });
            else
                _formatCombo.Items.AddRange(new object[] { "MP4", "WEBM", "MKV" });
            _formatCombo.SelectedIndex = 0;
        }

        private void UpdateStats()
        {
            int total = _queue.Count;
            int completed = _queue.Count(x => x.Status == DownloadStatus.Completed);
            int active = _queue.Count(x => x.Status == DownloadStatus.Downloading);
            _statsLabel.Text = $"{completed}/{total} completadas  •  {active} activas";
        }

        private void ShowToast(string msg)
        {
            var toast = new Label
            {
                Text = msg,
                Font = GetFont(10, FontStyle.Regular),
                ForeColor = White,
                BackColor = Color.FromArgb(200, 31, 41, 55),
                AutoSize = true,
                Padding = new Padding(14, 8, 14, 8)
            };
            toast.Left = (Width - toast.Width) / 2;
            toast.Top = Height - 80;
            Controls.Add(toast);
            toast.BringToFront();
            var t = new System.Windows.Forms.Timer { Interval = 2000 };
            t.Tick += (_, _) => { Controls.Remove(toast); t.Stop(); };
            t.Start();
        }

        // ── Paint ─────────────────────────────────────────────────────────────
        private void PaintSidebar(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(20, 255, 255, 255), 1f);
            g.DrawLine(pen, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
            using var brush = new SolidBrush(Red);
            g.FillRectangle(brush, (_sidebar.Width - 32) / 2, _sidebar.Height - 4, 32, 3);
        }

        private void PaintTopBar(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            using var pen = new Pen(Color.FromArgb(20, 255, 255, 255), 1f);
            g.DrawLine(pen, 0, _topBar.Height - 1, _topBar.Width, _topBar.Height - 1);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        internal static Font GetFont(float size, FontStyle style)
        {
            try { return new Font("Space Grotesk", size, style, GraphicsUnit.Point); }
            catch { return new Font("Segoe UI", size, style, GraphicsUnit.Point); }
        }

        private static Label MakeLabel(string text, float size, FontStyle style, Color color)
            => new Label
            {
                Text = text,
                Font = GetFont(size, style),
                ForeColor = color,
                BackColor = Color.Transparent,
                AutoSize = true
            };

        private ComboBox MakeCombo(string[] items, int left, int top)
        {
            var cb = new ComboBox
            {
                Font = GetFont(10, FontStyle.Regular),
                ForeColor = White,
                BackColor = Surface,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = left,
                Top = top,
                Width = 140,
                Height = 32
            };
            cb.Items.AddRange(items);
            return cb;
        }

        private static Button MakeIconButton(string icon, int left, int top)
            => new Button
            {
                Text = icon,
                Font = GetFont(12, FontStyle.Regular),
                ForeColor = Muted,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Left = left,
                Top = top,
                Width = 36,
                Height = 36,
                Cursor = Cursors.Hand
            };

        private static Label MakeWindowBtn(string text, Color hoverColor)
        {
            var btn = new Label
            {
                Text = text,
                Font = GetFont(9, FontStyle.Regular),
                ForeColor = Muted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Width = 28,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btn.MouseEnter += (_, _) => { btn.ForeColor = White; btn.BackColor = hoverColor; };
            btn.MouseLeave += (_, _) => { btn.ForeColor = Muted; btn.BackColor = Color.Transparent; };
            return btn;
        }
    }
}