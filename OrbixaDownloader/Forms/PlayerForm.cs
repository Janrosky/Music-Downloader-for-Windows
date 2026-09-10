using OrbixaDownloader.Models;
using WMPLib;

namespace OrbixaDownloader.Forms
{
    public class PlayerForm : Form
    {
        // ── Colors ────────────────────────────────────────────────────────────
        private static Color BgDeep => DrawHelper.Canvas;
        private static Color BgCard => DrawHelper.Glass;
        private static Color Red => DrawHelper.Accent;
        private static Color RedLight => DrawHelper.SurfaceStrong;
        private static Color White => DrawHelper.Text;
        private static Color Muted => DrawHelper.Muted;
        private static Color Surface => DrawHelper.Glass;
        private static Color Surface2 => DrawHelper.Field;

        // Extensiones soportadas para búsqueda en carpeta
        private static readonly string[] SupportedExtensions =
            { ".mp3", ".m4a", ".flac", ".wav", ".ogg", ".mp4", ".mkv", ".webm", ".avi" };

        // ── WMP ───────────────────────────────────────────────────────────────
        private WindowsMediaPlayer? _wmp;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly List<string> _playlist = new();
        private int _currentIndex = -1;
        private bool _isDraggingSeek = false;
        private bool _shuffle = false;
        private bool _repeat = false;
        private bool _isChangingTrack = false;

        // ── UI ────────────────────────────────────────────────────────────────
        private Panel _titleBar = null!;
        private Panel _artworkPanel = null!;
        private Label _artworkLabel = null!;
        private Label _titleLabel = null!;
        private Label _artistLabel = null!;
        private TrackBar _seekBar = null!;
        private Label _currentTime = null!;
        private Label _totalTime = null!;
        private Button _playPauseBtn = null!;
        private Button _shuffleBtn = null!;
        private Button _repeatBtn = null!;
        private TrackBar _volumeBar = null!;
        private FlowLayoutPanel _playlistPanel = null!;
        private System.Windows.Forms.Timer _progressTimer = null!;

        private bool _dragging;
        private Point _dragStart;

        public PlayerForm(IEnumerable<string>? initialFiles = null)
        {
            InitForm();
            BuildUI();
            UiText.Apply(this);
            DrawHelper.ApplyTheme(this);
            InitWMP();
            SetupTimer();

            if (initialFiles != null)
                foreach (var f in initialFiles)
                    AddToPlaylist(f);
        }

        // ── Init ──────────────────────────────────────────────────────────────
        private void InitForm()
        {
            Text = "Orbixa Player";
            Size = new Size(900, 640);
            MinimumSize = new Size(760, 560);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgDeep;
            DoubleBuffered = true;
            KeyPreview = true;
            KeyDown += OnKeyDown;
        }

        private void InitWMP()
        {
            try
            {
                _wmp = new WindowsMediaPlayer();
                _wmp.settings.autoStart = false;
                _wmp.settings.volume = _volumeBar.Value;
                _wmp.PlayStateChange += OnPlayStateChange;
                _wmp.MediaError += OnMediaError;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    UiText.Get($"No se pudo inicializar Windows Media Player:\n{ex.Message}\n\nAsegurate de tener WMP instalado y habilitado en Windows.\n\nEjecuta en PowerShell como Admin:\nEnable-WindowsOptionalFeature -Online -FeatureName 'WindowsMediaPlayer'", $"Windows Media Player could not be initialized:\n{ex.Message}\n\nMake sure WMP is installed and enabled in Windows.\n\nRun in PowerShell as Administrator:\nEnable-WindowsOptionalFeature -Online -FeatureName 'WindowsMediaPlayer'"),
                    "Orbixa Player", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SetupTimer()
        {
            _progressTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _progressTimer.Tick += (_, _) => UpdateProgress();
            _progressTimer.Start();
        }

        // ── Build UI ──────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // ── Title bar ─────────────────────────────────────────────────────
            _titleBar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = BgCard };
            _titleBar.Paint += (_, e) =>
            {
                using var pen = new Pen(DrawHelper.Border, 1f);
                e.Graphics.DrawLine(pen, 0, 47, _titleBar.Width, 47);
            };
            _titleBar.MouseDown += (_, e) => { _dragging = true; _dragStart = e.Location; };
            _titleBar.MouseMove += (_, e) =>
            {
                if (_dragging) Location = new Point(Left + e.X - _dragStart.X, Top + e.Y - _dragStart.Y);
            };
            _titleBar.MouseUp += (_, _) => _dragging = false;

            var appIcon = new Label
            {
                Text = "♪  ORBIXA PLAYER",
                Font = GetFont(11, FontStyle.Bold),
                ForeColor = White,
                AutoSize = true,
                Top = 14,
                Left = 20,
                BackColor = Color.Transparent
            };

            var closeBtn = MakeTitleBtn("✕", Red, Close);
            var minBtn = MakeTitleBtn("─", Surface, () => WindowState = FormWindowState.Minimized);
            var maxBtn = MakeTitleBtn("□", Surface, () =>
                WindowState = WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal : FormWindowState.Maximized);

            closeBtn.Anchor = minBtn.Anchor = maxBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            closeBtn.Top = minBtn.Top = maxBtn.Top = 8;

            _titleBar.Controls.AddRange(new Control[] { appIcon, closeBtn, minBtn, maxBtn });
            _titleBar.Resize += (_, _) =>
            {
                closeBtn.Left = _titleBar.Width - 40;
                maxBtn.Left = _titleBar.Width - 80;
                minBtn.Left = _titleBar.Width - 120;
            };

            // ── Left panel ────────────────────────────────────────────────────
            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 320, BackColor = BgDeep };

            // Artwork
            _artworkPanel = new Panel
            {
                Left = 24,
                Top = 24,
                Width = 272,
                Height = 272,
                BackColor = Surface2
            };
            DrawHelper.MakeRounded(_artworkPanel, 16);
            _artworkPanel.Paint += PaintArtwork;

            _artworkLabel = new Label
            {
                Text = "♪",
                Font = GetFont(64, FontStyle.Regular),
                ForeColor = DrawHelper.Muted,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            _artworkPanel.Controls.Add(_artworkLabel);

            // Track info
            _titleLabel = new Label
            {
                Text = "Sin reproducción",
                Font = GetFont(13, FontStyle.Bold),
                ForeColor = White,
                AutoSize = false,
                Left = 24,
                Top = 310,
                Width = 272,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            _artistLabel = new Label
            {
                Text = "—",
                Font = GetFont(10, FontStyle.Regular),
                ForeColor = Muted,
                AutoSize = false,
                Left = 24,
                Top = 338,
                Width = 272,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            // Seek
            _seekBar = new TrackBar
            {
                Left = 24,
                Top = 368,
                Width = 272,
                Minimum = 0,
                Maximum = 1000,
                TickStyle = TickStyle.None,
                BackColor = BgDeep
            };
            _seekBar.MouseDown += (_, _) => _isDraggingSeek = true;
            _seekBar.MouseUp += (_, _) => { _isDraggingSeek = false; SeekTo(_seekBar.Value / 1000.0); };

            var timeRow = new Panel { Left = 24, Top = 394, Width = 272, Height = 20, BackColor = Color.Transparent };
            _currentTime = new Label { Text = "0:00", Font = GetFont(8, FontStyle.Regular), ForeColor = Muted, AutoSize = true, Left = 0, Top = 2, BackColor = Color.Transparent };
            _totalTime = new Label { Text = "0:00", Font = GetFont(8, FontStyle.Regular), ForeColor = Muted, AutoSize = true, Left = 240, Top = 2, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            timeRow.Controls.AddRange(new Control[] { _currentTime, _totalTime });

            // Controls
            var ctrlPanel = new Panel { Left = 24, Top = 422, Width = 272, Height = 56, BackColor = Color.Transparent };
            _shuffleBtn = MakeCtrlBtn("⇄", 0, 16, Surface, () => ToggleShuffle(), 40, 40);
            var prevBtn = MakeCtrlBtn("⏮", 48, 8, Color.Transparent, PrevTrack, 40, 40);
            _playPauseBtn = MakeCtrlBtn("▶", 104, 0, Red, TogglePlayPause, 56, 56);
            var nextBtn = MakeCtrlBtn("⏭", 172, 8, Color.Transparent, NextTrack, 40, 40);
            _repeatBtn = MakeCtrlBtn("↺", 220, 16, Surface, () => ToggleRepeat(), 40, 40);
            DrawHelper.MakeRounded(_playPauseBtn, 28);
            ctrlPanel.Controls.AddRange(new Control[] { _shuffleBtn, prevBtn, _playPauseBtn, nextBtn, _repeatBtn });

            // Volume
            var volPanel = new Panel { Left = 24, Top = 490, Width = 272, Height = 28, BackColor = Color.Transparent };
            var volIcon = new Label { Text = "🔊", Font = GetFont(10, FontStyle.Regular), AutoSize = true, Top = 4, Left = 0, BackColor = Color.Transparent, ForeColor = Muted };
            _volumeBar = new TrackBar { Left = 30, Top = 2, Width = 238, Minimum = 0, Maximum = 100, Value = 80, TickStyle = TickStyle.None, BackColor = BgDeep };
            _volumeBar.ValueChanged += (_, _) => { if (_wmp != null) _wmp.settings.volume = _volumeBar.Value; };
            volPanel.Controls.AddRange(new Control[] { volIcon, _volumeBar });

            // ── Botones de carga — "Archivos" sólido + "Carpeta" outline ──────
            var addFilesBtn = MakeRedButton("➕  Archivos", 24, 530, 130, 36);
            var addFolderBtn = MakeOutlineButton("📂  Carpeta", 162, 530, 134, 36);
            addFilesBtn.Click += (_, _) => OpenFiles();
            addFolderBtn.Click += (_, _) => OpenFolder();

            leftPanel.Controls.AddRange(new Control[]
            {
                _artworkPanel, _titleLabel, _artistLabel,
                _seekBar, timeRow, ctrlPanel, volPanel,
                addFilesBtn, addFolderBtn
            });

            // ── Right: playlist ───────────────────────────────────────────────
            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgCard };

            var plHeader = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = BgCard };
            plHeader.Paint += (_, e) =>
            {
                using var pen = new Pen(DrawHelper.Border, 1f);
                e.Graphics.DrawLine(pen, 0, 51, plHeader.Width, 51);
            };

            var plTitle = new Label { Text = "Lista de reproducción", Font = GetFont(11, FontStyle.Bold), ForeColor = White, AutoSize = true, Top = 16, Left = 20, BackColor = Color.Transparent };
            var clearLbl = new Label { Text = "Limpiar todo", Font = GetFont(9, FontStyle.Regular), ForeColor = Muted, AutoSize = true, Top = 18, Cursor = Cursors.Hand, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            clearLbl.Click += (_, _) => ClearPlaylist();
            clearLbl.MouseEnter += (_, _) => clearLbl.ForeColor = Red;
            clearLbl.MouseLeave += (_, _) => clearLbl.ForeColor = Muted;
            plHeader.Controls.AddRange(new Control[] { plTitle, clearLbl });
            plHeader.Resize += (_, _) => clearLbl.Left = plHeader.Width - 100;

            _playlistPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(10, 8, 10, 8)
            };

            rightPanel.Controls.Add(_playlistPanel);
            rightPanel.Controls.Add(plHeader);

            Controls.Add(rightPanel);
            Controls.Add(leftPanel);
            Controls.Add(_titleBar);
        }

        private void PaintArtwork(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            DrawHelper.DrawGlow(g,
                _artworkPanel.Width / 2, _artworkPanel.Height / 2, 120,
                DrawHelper.PrimaryDark);
        }

        // ── Carga de archivos ─────────────────────────────────────────────────

        private void OpenFiles()
        {
            using var dlg = new OpenFileDialog
            {
                Title = UiText.Get("Agregar archivos", "Add files"),
                Filter = "Audio/Video|*.mp3;*.m4a;*.flac;*.wav;*.ogg;*.mp4;*.mkv;*.webm;*.avi|Todos|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                foreach (var f in dlg.FileNames)
                    AddToPlaylist(f);
        }

        /// <summary>
        /// Abre un FolderBrowserDialog y carga todos los archivos de audio/video
        /// de la carpeta seleccionada, ordenados alfabéticamente.
        /// Opcionalmente busca en subcarpetas si el usuario lo confirma.
        /// </summary>
        private void OpenFolder()
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = UiText.Get("Seleccioná una carpeta para cargar su música", "Select a folder to load music"),
                UseDescriptionForTitle = true
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string folder = dlg.SelectedPath;

            // Preguntar si incluir subcarpetas
            var includeSubfolders = MessageBox.Show(
                UiText.Get("¿Incluir archivos de subcarpetas también?", "Include files from subfolders too?"),
                "Orbixa Player",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;

            var searchOption = includeSubfolders
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            // Buscar todos los archivos con extensión soportada, orden alfabético
            var files = Directory.EnumerateFiles(folder, "*.*", searchOption)
                .Where(f => SupportedExtensions.Contains(
                    Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (files.Count == 0)
            {
                MessageBox.Show(
                    UiText.Get("No se encontraron archivos de audio o video en esa carpeta.", "No audio or video files were found in that folder."),
                    "Orbixa Player", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var f in files)
                AddToPlaylist(f);
        }

        // ── Playlist ──────────────────────────────────────────────────────────
        public void AddToPlaylist(string filePath)
        {
            if (!File.Exists(filePath)) return;
            int idx = _playlist.Count;
            _playlist.Add(filePath);
            AddPlaylistRow(filePath, idx);
            if (_currentIndex == -1) PlayAt(0);
        }

        private void AddPlaylistRow(string filePath, int index)
        {
            string name = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath).TrimStart('.').ToUpper();
            bool isVideo = ext is "MP4" or "MKV" or "WEBM" or "AVI";

            var row = new Panel
            {
                Width = _playlistPanel.Width - 24,
                Height = 52,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4),
                Cursor = Cursors.Hand,
                Tag = index
            };
            row.Paint += (s, e) =>
            {
                var p = (Panel)s!; int idx = (int)p.Tag!; bool cur = idx == _currentIndex;
                var r = new Rectangle(0, 0, p.Width - 2, p.Height - 2);
                DrawHelper.FillRounded(e.Graphics, r, 10,
                    cur ? DrawHelper.SurfaceStrong : DrawHelper.Glass);
                if (cur) DrawHelper.DrawRounded(e.Graphics, r, 10, DrawHelper.BorderHighlight, 1f);
            };
            row.Click += (_, _) => PlayAt((int)row.Tag!);

            var num = new Label
            {
                Text = $"{index + 1}",
                Font = GetFont(9, FontStyle.Regular),
                ForeColor = DrawHelper.Muted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Width = 28,
                Height = 52,
                Left = 8,
                Top = 0,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var badge = new Label
            {
                Text = ext,
                Font = GetFont(7, FontStyle.Bold),
                ForeColor = isVideo ? DrawHelper.Focus : Red,
                BackColor = isVideo ? DrawHelper.SurfaceStrong : DrawHelper.Glass,
                AutoSize = false,
                Width = 38,
                Height = 18,
                Left = 40,
                Top = 17,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var nameLbl = new Label
            {
                Text = name.Length > 36 ? name[..33] + "…" : name,
                Font = GetFont(10, FontStyle.Regular),
                ForeColor = White,
                BackColor = Color.Transparent,
                AutoSize = false,
                Width = 250,
                Height = 52,
                Left = 84,
                Top = 0,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var xBtn = new Label
            {
                Text = "✕",
                Font = GetFont(9, FontStyle.Regular),
                ForeColor = DrawHelper.Muted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Width = 28,
                Height = 52,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            xBtn.MouseEnter += (_, _) => xBtn.ForeColor = Red;
            xBtn.MouseLeave += (_, _) => xBtn.ForeColor = DrawHelper.Muted;
            xBtn.Click += (_, _) => RemoveAt((int)row.Tag!);

            row.Controls.AddRange(new Control[] { num, badge, nameLbl, xBtn });
            row.Resize += (_, _) => { nameLbl.Width = row.Width - 140; xBtn.Left = row.Width - 32; };

            _playlistPanel.Controls.Add(row);
        }

        private void RefreshHighlight() =>
            _playlistPanel.Controls.Cast<Control>().ToList().ForEach(c => c.Invalidate());

        private void ClearPlaylist()
        {
            _wmp?.controls.stop();
            _playlist.Clear();
            _currentIndex = -1;
            _playlistPanel.Controls.Clear();
            _titleLabel.Text = "Sin reproducción";
            _artistLabel.Text = "—";
            _currentTime.Text = "0:00";
            _totalTime.Text = "0:00";
            _seekBar.Value = 0;
            _playPauseBtn.Text = "▶";
            _playPauseBtn.BackColor = Surface;
            _artworkPanel.Invalidate();
        }

        private void RemoveAt(int index)
        {
            if (index < 0 || index >= _playlist.Count) return;
            bool wasPlaying = index == _currentIndex;
            _playlist.RemoveAt(index);
            _playlistPanel.Controls.Clear();
            for (int i = 0; i < _playlist.Count; i++) AddPlaylistRow(_playlist[i], i);
            if (wasPlaying) { _currentIndex = -1; if (_playlist.Count > 0) PlayAt(Math.Min(index, _playlist.Count - 1)); }
            else if (_currentIndex > index) _currentIndex--;
        }

        // ── Playback ──────────────────────────────────────────────────────────
        private void PlayAt(int index)
        {
            if (_wmp == null || index < 0 || index >= _playlist.Count) return;
            _isChangingTrack = true;
            try
            {
                _wmp.controls.stop();
                _wmp.URL = "";

                _currentIndex = index;
                string path = _playlist[index];
                string name = Path.GetFileNameWithoutExtension(path);
                string ext = Path.GetExtension(path).TrimStart('.').ToUpper();

                _titleLabel.Text = name.Length > 28 ? name[..25] + "…" : name;
                _artistLabel.Text = ext;

                IWMPMedia media = _wmp.newMedia(path);
                _wmp.currentMedia = media;
                _wmp.controls.play();

                _playPauseBtn.Text = "⏸";
                _playPauseBtn.BackColor = Red;

                RefreshHighlight();
                _artworkPanel.Invalidate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlayAt] Error: {ex.Message}");
                MessageBox.Show(UiText.Get($"No se pudo reproducir el archivo:\n{ex.Message}", $"The file could not be played:\n{ex.Message}"),
                    "Orbixa Player", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _isChangingTrack = false; }
        }

        private void TogglePlayPause()
        {
            if (_wmp == null || _currentIndex == -1) return;
            if (_wmp.playState == WMPPlayState.wmppsPlaying)
            {
                _wmp.controls.pause();
                _playPauseBtn.Text = "▶"; _playPauseBtn.BackColor = Surface;
            }
            else
            {
                _wmp.controls.play();
                _playPauseBtn.Text = "⏸"; _playPauseBtn.BackColor = Red;
            }
        }

        private void NextTrack()
        {
            if (_playlist.Count == 0) return;
            int next = _shuffle
                ? new Random().Next(_playlist.Count)
                : (_currentIndex + 1) % _playlist.Count;
            PlayAt(next);
        }

        private void PrevTrack()
        {
            if (_playlist.Count == 0) return;
            if (_wmp != null &&
                _wmp.playState != WMPPlayState.wmppsStopped &&
                _wmp.controls.currentPosition > 3)
                _wmp.controls.currentPosition = 0;
            else
                PlayAt((_currentIndex - 1 + _playlist.Count) % _playlist.Count);
        }

        private void SeekTo(double fraction)
        {
            if (_wmp == null) return;
            var state = _wmp.playState;
            if (state != WMPPlayState.wmppsPlaying && state != WMPPlayState.wmppsPaused) return;
            double dur = _wmp.currentMedia?.duration ?? 0;
            if (dur > 0) _wmp.controls.currentPosition = fraction * dur;
        }

        private void ToggleShuffle()
        {
            _shuffle = !_shuffle;
            _shuffleBtn.BackColor = _shuffle ? DrawHelper.SurfaceStrong : Surface;
            _shuffleBtn.ForeColor = _shuffle ? Red : Muted;
        }

        private void ToggleRepeat()
        {
            _repeat = !_repeat;
            _repeatBtn.BackColor = _repeat ? DrawHelper.SurfaceStrong : Surface;
            _repeatBtn.ForeColor = _repeat ? Red : Muted;
        }

        private void UpdateProgress()
        {
            if (_wmp == null || _isDraggingSeek || _isChangingTrack) return;
            try
            {
                var state = _wmp.playState;
                if (state != WMPPlayState.wmppsPlaying && state != WMPPlayState.wmppsPaused) return;
                double pos = _wmp.controls.currentPosition;
                double dur = _wmp.currentMedia?.duration ?? 0;
                _currentTime.Text = FormatTime(pos);
                _totalTime.Text = FormatTime(dur);
                if (dur > 0)
                {
                    _seekBar.Value = (int)(pos / dur * 1000);
                    _totalTime.Left = _seekBar.Width - _totalTime.Width;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[UpdateProgress] {ex.Message}"); }
        }

        private void OnPlayStateChange(int newState)
        {
            if (newState == 8)
            {
                if (IsDisposed || !IsHandleCreated) return;
                try { BeginInvoke(() => { if (_repeat) PlayAt(_currentIndex); else NextTrack(); }); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnPlayStateChange] {ex.Message}"); }
            }
        }

        private void OnMediaError(object pMediaObject)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke(() =>
                {
                    string errorInfo = "Archivo no compatible o codec faltante.";
                    if (pMediaObject is IWMPMedia m)
                        errorInfo = $"Archivo: {Path.GetFileName(m.sourceURL)}\n{errorInfo}";
                    MessageBox.Show(
                        UiText.Get($"Error al reproducir:\n{errorInfo}\n\nAsegurate de tener los codecs necesarios instalados (K-Lite Codec Pack).", $"Playback error:\n{errorInfo}\n\nMake sure the required codecs are installed (K-Lite Codec Pack)."),
                        "Orbixa Player", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
            }
            catch { }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space: TogglePlayPause(); e.Handled = true; break;
                case Keys.Right: SeekTo(Math.Min(1.0, (_seekBar.Value + 50) / 1000.0)); break;
                case Keys.Left: SeekTo(Math.Max(0.0, (_seekBar.Value - 50) / 1000.0)); break;
                case Keys.Escape: Close(); break;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static string FormatTime(double s)
        {
            if (s < 0 || double.IsNaN(s)) return "0:00";
            var t = TimeSpan.FromSeconds(s);
            return t.Hours > 0
                ? $"{t.Hours}:{t.Minutes:D2}:{t.Seconds:D2}"
                : $"{t.Minutes}:{t.Seconds:D2}";
        }

        private static Button MakeTitleBtn(string text, Color hoverBg, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = GetFont(9, FontStyle.Regular),
                ForeColor = Muted,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(32, 32),
                Top = 8,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = hoverBg;
            btn.Click += (_, _) => onClick();
            return btn;
        }

        private static Button MakeCtrlBtn(string icon, int left, int top,
            Color bg, Action onClick, int w, int h)
        {
            var btn = new Button
            {
                Text = icon,
                Font = GetFont(w >= 56 ? 18 : 13, FontStyle.Regular),
                ForeColor = bg == Red ? White : Muted,
                BackColor = bg == Color.Transparent ? Color.Transparent : bg,
                FlatStyle = FlatStyle.Flat,
                Left = left,
                Top = top,
                Width = w,
                Height = h,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = DrawHelper.SurfaceStrong;
            btn.Click += (_, _) => onClick();
            DrawHelper.MakeRounded(btn, h / 2);
            return btn;
        }

        private Button MakeRedButton(string text, int left, int top, int w, int h)
        {
            var btn = new Button
            {
                Text = text,
                Font = GetFont(10, FontStyle.Bold),
                ForeColor = White,
                BackColor = Red,
                FlatStyle = FlatStyle.Flat,
                Left = left,
                Top = top,
                Width = w,
                Height = h,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = RedLight;
            DrawHelper.MakeRounded(btn, 10);
            return btn;
        }

        /// Botón outline con borde rojo sutil — diferencia visualmente la acción de carpeta
        private Button MakeOutlineButton(string text, int left, int top, int w, int h)
        {
            var btn = new Button
            {
                Text = text,
                Font = GetFont(10, FontStyle.Regular),
                ForeColor = Red,
                BackColor = DrawHelper.Glass,
                FlatStyle = FlatStyle.Flat,
                Left = left,
                Top = top,
                Width = w,
                Height = h,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = DrawHelper.Border;
            btn.FlatAppearance.MouseOverBackColor = DrawHelper.SurfaceStrong;
            DrawHelper.MakeRounded(btn, 10);
            return btn;
        }

        internal static Font GetFont(float size, FontStyle style) =>
            MainForm.GetFont(size, style);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _progressTimer?.Dispose();
                if (_wmp != null)
                {
                    try
                    {
                        _wmp.controls.stop();
                        _wmp.PlayStateChange -= OnPlayStateChange;
                        _wmp.MediaError -= OnMediaError;
                    }
                    catch { }
                    finally
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(_wmp);
                        _wmp = null;
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}
