using OrbixaDownloader.Models;
using OrbixaDownloader.Services;

namespace OrbixaDownloader.Forms
{
    public partial class MainForm : Form
    {
        // ── Colors ────────────────────────────────────────────────────────────
        internal static Color BgDeep => DrawHelper.Canvas;
        internal static Color BgCard => DrawHelper.Glass;
        internal static Color Red => DrawHelper.Accent;
        internal static Color RedLight => DrawHelper.SurfaceStrong;
        internal static Color White => DrawHelper.Text;
        internal static Color Muted => DrawHelper.Muted;
        internal static Color Surface => DrawHelper.Glass;
        internal static Color Surface2 => DrawHelper.Field;

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
        private Label _feedback = null!;
        private Label _destination = null!;
        private Label _emptyQueue = null!;
        private string _audioChoice = "MP3", _videoChoice = "MP4";
        private bool _changingDefaults;
        private readonly System.Windows.Forms.Timer _statsTimer = new() { Interval = 250 };

        // ── Tooltip ───────────────────────────────────────────────────────────
        private readonly ToolTip _toolTip = new ToolTip();

        // ── Drag ──────────────────────────────────────────────────────────────
        private bool _dragging;
        private Point _dragStart;

        public MainForm(AppSettings settings)
        {
            _settings = settings;
            _settings.Normalize();
            DrawHelper.SetTheme(_settings.Theme, _settings.CustomTheme);
            _downloader = new DownloadService(settings);
            _metadata = new MetadataService(settings);
            InitializeComponent();
            BuildUI();
            ApplyDefaults();
            _statsTimer.Tick += (_, _) => UpdateStats();
            _statsTimer.Start();
            FormClosed += (_, _) => _statsTimer.Dispose();
        }

        // ── Build UI ──────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // ── Sidebar ───────────────────────────────────────────────────────
            _sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = DrawHelper.RaisedCanvas,
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
                var btn = new Button
                {
                    Text = item,
                    Font = GetFont(10, FontStyle.Regular),
                    ForeColor = item.StartsWith("⬇") ? White : Muted,
                    BackColor = item.StartsWith("⬇") ? DrawHelper.SurfaceStrong : Color.Transparent,
                    AutoSize = false,
                    Width = 180,
                    Height = 38,
                    Left = 20,
                    Top = navY,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AccessibleName = item,
                    Padding = new Padding(16, 0, 0, 0),
                    Cursor = Cursors.Hand
                };

                // Navegación entre secciones
                if (item.StartsWith("⚙"))
                    btn.Click += (_, _) =>
                    {
                        using var sf = new SettingsForm(_settings);
                        sf.ShowDialog(this);
                        ApplyDefaults();
                    };

                if (item.StartsWith("⬇")) btn.Click += (_, _) => _urlBox.Focus();
                if (item.StartsWith("📋")) btn.Click += (_, _) => _queuePanel.Focus();
                DrawHelper.AddRoundedAppearance(btn, 8);
                _sidebar.Controls.Add(btn);
                navY += 48;
            }

            var verLabel = new Label
            {
                Text = "v1.0.0",
                Font = GetFont(8, FontStyle.Regular),
                ForeColor = DrawHelper.Muted,
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
                BackColor = DrawHelper.RaisedCanvas,
            };
            _topBar.Paint += PaintTopBar;
            _topBar.MouseDown += (_, e) => { _dragging = true; _dragStart = e.Location; };
            _topBar.MouseMove += (_, e) => { if (_dragging) Location = new Point(Left + e.X - _dragStart.X, Top + e.Y - _dragStart.Y); };
            _topBar.MouseUp += (_, _) => _dragging = false;

            var closeBtn = MakeWindowBtn("✕", DrawHelper.Accent);
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
                BackColor = DrawHelper.SurfaceStrong,
                FlatStyle = FlatStyle.Flat,
                Width = 130,
                Height = 30,
                Top = 11,
                Left = 350,
                Cursor = Cursors.Hand
            };
            playerBtn.FlatAppearance.BorderSize = 0;
            playerBtn.FlatAppearance.MouseOverBackColor = DrawHelper.Accent;
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
                Left = 16
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

            var addSub = MakeLabel("Pegá un enlace de YouTube o SoundCloud (un elemento)", 10, FontStyle.Regular, Muted);
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

            _urlBox.AccessibleName = "Enlace de descarga";
            _urlBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; OnDownloadClicked(_downloadBtn, EventArgs.Empty); } };

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

            _formatCombo.SelectedIndexChanged += (_, _) =>
            {
                if (_changingDefaults) return;
                if (_typeCombo.SelectedIndex == 0) _audioChoice = _formatCombo.Text;
                else _videoChoice = _formatCombo.Text;
            };

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

            _feedback = new Label { Left = 0, Top = 225, Height = 42, ForeColor = Muted, AutoEllipsis = true, Text = "Usá un enlace completo que empiece con https://", AccessibleName = "Ayuda y resultado de validación" };
            _destination = new Label { Left = 215, Top = 176, Height = 44, ForeColor = Muted, AutoEllipsis = true, AccessibleName = "Carpeta de destino" };
            _contentArea.Controls.Add(_feedback);
            _contentArea.Controls.Add(_destination);

            // Divider
            var divider = new Panel
            {
                Left = 0,
                Top = 272,
                Height = 1,
                BackColor = DrawHelper.Border,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            _queueTitle = MakeLabel("Cola de descargas", 12, FontStyle.Bold, White);
            _queueTitle.Top = 280; _queueTitle.Left = 0;

            _queuePanel = new FlowLayoutPanel
            {
                Left = 0,
                Top = 310,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
            };

            _queuePanel.TabStop = true;
            _queuePanel.AccessibleName = "Cola de descargas";
            _emptyQueue = new Label { Text = "No hay descargas. Pegá un enlace para empezar.", AutoSize = true, ForeColor = Muted, Padding = new Padding(0, 12, 0, 0) };
            _queuePanel.Controls.Add(_emptyQueue);

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
                _queuePanel.Height = Math.Max(100, _contentArea.Height - 330);
                _feedback.Width = _contentArea.Width - 64;
                _destination.Width = Math.Max(100, _contentArea.Width - 280);
                foreach (Control card in _queuePanel.Controls) card.Width = Math.Max(200, _queuePanel.ClientSize.Width - 24);
            };

            Controls.Add(_contentArea);
            Controls.Add(_topBar);
            Controls.Add(_sidebar);
        }

        // ── Download logic ────────────────────────────────────────────────────
        private void OnDownloadClicked(object? sender, EventArgs e)
        {
            string url = _urlBox.Text.Trim();
            if (!DownloadService.IsSupportedUrl(url))
            {
                _feedback.Text = UiText.Get("Ingresá una URL válida de http:// o https:// con un sitio web.", "Enter a valid http:// or https:// website URL.");
                _urlBox.Focus(); return;
            }
            var options = BuildOptions();
            try { _downloader.ValidatePreflight(options); }
            catch (Exception ex) { _feedback.Text = ex.Message; _toolTip.SetToolTip(_feedback, ex.Message); return; }
            _downloadBtn.Enabled = false;
            _downloadBtn.Text = "Agregando...";
            try
            {
                var item = new DownloadItem { Url = url, CancellationSource = new CancellationTokenSource(), Options = options.Snapshot() };
                _queue.Add(item);
                var card = new DownloadCard(item, OnCancelItem, OnOpenFolder, OnRetryItem) { Width = Math.Max(200, _queuePanel.ClientSize.Width - 24) };
                _queuePanel.Controls.Add(card);
                _urlBox.Clear();
                _feedback.Text = UiText.Localize("Descarga agregada a la cola.");
                _ = RunDownloadAsync(item);
            }
            finally { _downloadBtn.Enabled = true; _downloadBtn.Text = "  ⬇  Descargar"; UpdateStats(); }
        }

        private async Task RunDownloadAsync(DownloadItem item)
        {
            await _downloader.DownloadAsync(item, item.Options!);
            if (!IsDisposed) UpdateStats();
        }

        private void OnRetryItem(DownloadItem item)
        {
            if (item.Status is not (DownloadStatus.Failed or DownloadStatus.Cancelled)) return;
            item.CancellationSource?.Dispose();
            item.CancellationSource = new CancellationTokenSource();
            item.Status = DownloadStatus.Pending;
            item.ErrorDetails = "";
            item.Progress = 0;
            _ = RunDownloadAsync(item);
        }
        private DownloadOptions BuildOptions()
        {
            bool isAudio = _typeCombo.SelectedIndex == 0;
            var opts = new DownloadOptions
            {
                Type = isAudio ? DownloadType.AudioOnly : DownloadType.VideoAndAudio,
                OutputFolder = _settings.DefaultOutputFolder,
                EmbedThumbnail = _settings.EmbedThumbnail,
                EmbedMetadata = _settings.EmbedMetadata,
                Quality = _settings.DefaultQuality,
            };

            if (isAudio)
            {
                opts.AudioFormat = _formatCombo.SelectedItem?.ToString() switch
                {
                    "M4A" => AudioFormat.M4A,
                    "FLAC" => AudioFormat.FLAC,
                    "WAV" => AudioFormat.WAV,
                    "OGG" => AudioFormat.OGG,
                    _ => AudioFormat.MP3
                };
            }
            else if (Enum.TryParse<VideoFormat>(_formatCombo.Text, out var videoFormat)) opts.VideoFormat = videoFormat;
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

        private void ApplyDefaults()
        {
            UiText.Language = _settings.Language;
            DrawHelper.SetTheme(_settings.Theme, _settings.CustomTheme);
            DrawHelper.ApplyTheme(this);
            UiText.Apply(this);
            _changingDefaults = true;
            _audioChoice = _settings.DefaultAudioFormat.ToString();
            _videoChoice = _settings.DefaultVideoFormat.ToString();
            _typeCombo.SelectedIndex = _settings.DefaultDownloadType == DownloadType.AudioOnly ? 0 : 1;
            UpdateFormatCombo();
            _changingDefaults = false;
            _destination.Text = UiText.Get("Destino: ", "Destination: ") + _settings.DefaultOutputFolder;
            _toolTip.SetToolTip(_destination, _settings.DefaultOutputFolder);
        }

        private void UpdateFormatCombo()
        {
            if (_formatCombo == null) return;
            bool previous = _changingDefaults;
            _changingDefaults = true;
            _formatCombo.Items.Clear();
            if (_typeCombo.SelectedIndex == 0) _formatCombo.Items.AddRange(new object[] { "MP3", "M4A", "FLAC", "WAV", "OGG" });
            else _formatCombo.Items.AddRange(new object[] { "MP4", "WEBM", "MKV" });
            _formatCombo.SelectedItem = _typeCombo.SelectedIndex == 0 ? _audioChoice : _videoChoice;
            if (_formatCombo.SelectedIndex < 0) _formatCombo.SelectedIndex = 0;
            _changingDefaults = previous;
        }

        private void UpdateStats()
        {
            int completed = _queue.Count(x => x.Status == DownloadStatus.Completed);
            int active = _queue.Count(x => x.Status is DownloadStatus.FetchingInfo or DownloadStatus.Downloading or DownloadStatus.Converting);
            int pending = _queue.Count(x => x.Status == DownloadStatus.Pending);
            _statsLabel.Text = UiText.Get($"{completed}/{_queue.Count} completadas · {active} activas · {pending} en cola",
                $"{completed}/{_queue.Count} completed · {active} active · {pending} queued");
            _emptyQueue.Visible = _queue.Count == 0;
        }

        private void ShowToast(string msg)
        {
            var toast = new Label
            {
                Text = msg,
                Font = GetFont(10, FontStyle.Regular),
                ForeColor = White,
                BackColor = DrawHelper.SurfaceSolid,
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
            using var pen = new Pen(DrawHelper.Border, 1f);
            g.DrawLine(pen, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
            using var brush = new SolidBrush(Red);
            g.FillRectangle(brush, (_sidebar.Width - 32) / 2, _sidebar.Height - 4, 32, 3);
        }

        private void PaintTopBar(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            using var pen = new Pen(DrawHelper.Border, 1f);
            g.DrawLine(pen, 0, _topBar.Height - 1, _topBar.Width, _topBar.Height - 1);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        internal static Font GetFont(float size, FontStyle style)
        {
            try { return new Font("Segoe UI", size, style, GraphicsUnit.Point); }
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


