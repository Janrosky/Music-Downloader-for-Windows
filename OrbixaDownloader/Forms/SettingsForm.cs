using OrbixaDownloader.Models;
using OrbixaDownloader.Services;

namespace OrbixaDownloader.Forms
{
    public class SettingsForm : Form
    {
        private readonly AppSettings _settings;
        private readonly UpdaterService _updater;

        private static readonly Color BgDeep = Color.FromArgb(5, 8, 22);
        private static readonly Color BgCard = Color.FromArgb(13, 18, 36);
        private static readonly Color Red = Color.FromArgb(219, 41, 85);
        private static readonly Color RedLight = Color.FromArgb(230, 64, 108);
        private static readonly Color White = Color.FromArgb(244, 244, 245);
        private static readonly Color Muted = Color.FromArgb(161, 161, 170);
        private static readonly Color Surface = Color.FromArgb(31, 41, 55);

        // FIX: Ancho interno real del scroll = 560 - 2*28 padding = 504px
        // Todos los controles internos deben usar este ancho como referencia
        private const int InnerWidth = 504;

        private bool _dragging;
        private Point _dragStart;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;
            _updater = new UpdaterService(settings);
            InitForm();
            BuildUI();
        }

        private void InitForm()
        {
            Text = "Ajustes — Orbixa";
            Size = new Size(560, 600);
            MinimumSize = new Size(560, 600);
            MaximumSize = new Size(560, 600);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = BgDeep;
            DoubleBuffered = true;
            KeyPreview = true;
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }

        private void BuildUI()
        {
            // ── Title bar ─────────────────────────────────────────────────────
            // FIX: Reemplazada la barra mínima por la misma estructura del PlayerForm
            // con botones de minimizar y cerrar alineados por Resize
            var titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = BgCard
            };
            titleBar.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(20, 255, 255, 255), 1f);
                e.Graphics.DrawLine(pen, 0, 47, titleBar.Width, 47);
            };

            // Drag desde title bar
            titleBar.MouseDown += (_, e) => { _dragging = true; _dragStart = e.Location; };
            titleBar.MouseMove += (_, e) =>
            {
                if (_dragging)
                    Location = new Point(Left + e.X - _dragStart.X, Top + e.Y - _dragStart.Y);
            };
            titleBar.MouseUp += (_, _) => _dragging = false;

            var appIcon = new Label
            {
                Text = "⚙  ORBIXA · AJUSTES",
                Font = GetFont(11, FontStyle.Bold),
                ForeColor = White,
                AutoSize = true,
                Top = 14,
                Left = 20,
                BackColor = Color.Transparent
            };

            // FIX: Botones de control iguales al PlayerForm, con Anchor + Resize
            var closeBtn = MakeTitleBtn("✕", Red, Close);
            var minBtn = MakeTitleBtn("─", Surface, () => WindowState = FormWindowState.Minimized);

            closeBtn.Anchor = minBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            closeBtn.Top = minBtn.Top = 8;

            titleBar.Controls.AddRange(new Control[] { appIcon, closeBtn, minBtn });

            // FIX: Posicionamiento dinámico con Resize para que no queden hardcodeados
            titleBar.Resize += (_, _) =>
            {
                closeBtn.Left = titleBar.Width - 40;
                minBtn.Left = titleBar.Width - 80;
            };
            // Forzar posición inicial (el Resize no dispara al primer render)
            closeBtn.Left = 560 - 40;
            minBtn.Left = 560 - 80;

            // ── Scroll container ──────────────────────────────────────────────
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BgDeep,
                // FIX: Padding uniforme — todos los controles hijos usan Left=0
                // y el panel aplica 28px de margen horizontal automáticamente
                Padding = new Padding(28, 20, 28, 20)
            };

            // Drag desde el fondo del scroll también
            scroll.MouseDown += (_, e) => { _dragging = true; _dragStart = e.Location; };
            scroll.MouseMove += (_, e) =>
            {
                if (_dragging)
                    Location = new Point(Left + e.X - _dragStart.X, Top + e.Y - _dragStart.Y);
            };
            scroll.MouseUp += (_, _) => _dragging = false;

            int y = 8;

            // ── Carpeta de destino ────────────────────────────────────────────
            y = AddSectionHeader(scroll, "📁  Carpeta de destino", y);

            // FIX: folderBox envuelto en un panel contenedor con borde sutil
            // para que se vea correctamente con BorderStyle.None
            var folderContainer = new Panel
            {
                Left = 0,
                Top = y,
                // FIX: Ancho calculado para dejar espacio al botón (110px) + gap (8px)
                Width = InnerWidth - 118,
                Height = 34,
                BackColor = Surface
            };
            DrawHelper.MakeRounded(folderContainer, 8);

            var folderBox = new TextBox
            {
                Text = _settings.DefaultOutputFolder,
                Font = GetFont(9, FontStyle.Regular),
                ForeColor = White,
                BackColor = Surface,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                // FIX: Padding interno para que el texto no quede pegado al borde
                Margin = new Padding(8, 0, 8, 0)
            };
            // TextBox no respeta Padding con Dock.Fill — usar Location manual
            folderBox.Location = new Point(8, 8);
            folderBox.Width = folderContainer.Width - 16;
            folderContainer.Controls.Add(folderBox);

            // FIX: browseBtn posicionado relativo al InnerWidth, no hardcodeado
            var browseBtn = MakeRedButton("📂  Examinar", InnerWidth - 110, y - 1, 110, 36);
            browseBtn.Click += (_, _) =>
            {
                using var dlg = new FolderBrowserDialog
                {
                    Description = "Seleccioná la carpeta de destino",
                    SelectedPath = _settings.DefaultOutputFolder,
                    UseDescriptionForTitle = true
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    folderBox.Text = dlg.SelectedPath;
                    _settings.DefaultOutputFolder = dlg.SelectedPath;
                    _settings.Save();
                }
            };

            scroll.Controls.AddRange(new Control[] { folderContainer, browseBtn });
            y += 50;

            // ── Formato por defecto ───────────────────────────────────────────
            y = AddSectionHeader(scroll, "🎵  Formato por defecto", y + 8);

            // FIX: Labels y combos alineados con espaciado consistente
            AddFieldLabel(scroll, "Audio:", 0, y + 7);
            var audioCombo = MakeCombo(scroll,
                new[] { "MP3", "M4A", "FLAC", "WAV" },
                _settings.DefaultAudioFormat.ToString(),
                52, y, 110);
            audioCombo.SelectedIndexChanged += (_, _) =>
            {
                if (Enum.TryParse<AudioFormat>(audioCombo.SelectedItem?.ToString(), out var fmt))
                { _settings.DefaultAudioFormat = fmt; _settings.Save(); }
            };

            AddFieldLabel(scroll, "Video:", 186, y + 7);
            var videoCombo = MakeCombo(scroll,
                new[] { "MP4", "WEBM", "MKV" },
                _settings.DefaultVideoFormat.ToString(),
                238, y, 110);
            videoCombo.SelectedIndexChanged += (_, _) =>
            {
                if (Enum.TryParse<VideoFormat>(videoCombo.SelectedItem?.ToString(), out var fmt))
                { _settings.DefaultVideoFormat = fmt; _settings.Save(); }
            };

            y += 50;

            // ── Metadata ──────────────────────────────────────────────────────
            y = AddSectionHeader(scroll, "🏷  Metadata", y + 8);

            var embedThumb = AddToggle(scroll,
                "Incrustar thumbnail en el archivo", _settings.EmbedThumbnail, y);
            embedThumb.CheckedChanged += (_, _) =>
            { _settings.EmbedThumbnail = embedThumb.Checked; _settings.Save(); };
            y += 36;

            var embedMeta = AddToggle(scroll,
                "Incrustar metadata (título, artista)", _settings.EmbedMetadata, y);
            embedMeta.CheckedChanged += (_, _) =>
            { _settings.EmbedMetadata = embedMeta.Checked; _settings.Save(); };
            y += 48;

            // ── Rendimiento ───────────────────────────────────────────────────
            y = AddSectionHeader(scroll, "⚡  Rendimiento", y + 8);

            AddFieldLabel(scroll, "Descargas simultáneas:", 0, y + 7);
            var concurrentCombo = MakeCombo(scroll,
                new[] { "1", "2", "3", "4" },
                _settings.MaxConcurrentDownloads.ToString(),
                180, y, 80);
            concurrentCombo.SelectedIndexChanged += (_, _) =>
            {
                if (int.TryParse(concurrentCombo.SelectedItem?.ToString(), out int n))
                { _settings.MaxConcurrentDownloads = n; _settings.Save(); }
            };
            y += 50;

            // ── Actualizaciones ───────────────────────────────────────────────
            y = AddSectionHeader(scroll, "🔄  Actualizaciones", y + 8);

            var autoUpdate = AddToggle(scroll,
                "Verificar actualizaciones al iniciar", _settings.CheckUpdatesOnStartup, y);
            autoUpdate.CheckedChanged += (_, _) =>
            { _settings.CheckUpdatesOnStartup = autoUpdate.Checked; _settings.Save(); };
            y += 44;

            // FIX: verLabel con color de acento sutil para distinguirse del texto muted
            var verLabel = new Label
            {
                Text = $"yt-dlp instalado:  v{_settings.InstalledYtDlpVersion}",
                Font = GetFont(9, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 219, 41, 85), // acento rojo tenue
                AutoSize = true,
                Left = 0,
                Top = y,
                BackColor = Color.Transparent
            };
            scroll.Controls.Add(verLabel);
            y += 34;

            var updateNowBtn = MakeRedButton("🔄  Actualizar ahora", 0, y, 190, 36);
            updateNowBtn.Click += async (_, _) =>
            {
                updateNowBtn.Enabled = false;
                updateNowBtn.Text = "⏳  Actualizando...";
                await _updater.CheckAndUpdateAllAsync();
                verLabel.Text = $"yt-dlp instalado:  v{_settings.InstalledYtDlpVersion}";
                updateNowBtn.Enabled = true;
                updateNowBtn.Text = "🔄  Actualizar ahora";
            };
            scroll.Controls.Add(updateNowBtn);
            y += 52;

            // FIX: Spacer final para que el último botón no quede pegado al borde
            var spacer = new Panel { Left = 0, Top = y, Width = 1, Height = 12, BackColor = Color.Transparent };
            scroll.Controls.Add(spacer);

            // FIX: Orden correcto — scroll va antes del titleBar para que la barra
            // quede siempre encima del contenido al hacer scroll
            Controls.Add(scroll);
            Controls.Add(titleBar);
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private int AddSectionHeader(Panel parent, string title, int y)
        {
            var lbl = new Label
            {
                Text = title,
                Font = GetFont(10, FontStyle.Bold),
                ForeColor = White,
                AutoSize = true,
                Left = 0,
                Top = y,
                BackColor = Color.Transparent
            };

            // FIX: line usa InnerWidth en lugar de 490 hardcodeado
            var line = new Panel
            {
                Left = 0,
                Top = y + 26,
                Height = 1,
                // FIX: Anclar al ancho real del contenedor para que no se salga
                Width = InnerWidth,
                BackColor = Color.FromArgb(25, 255, 255, 255),
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };

            parent.Controls.AddRange(new Control[] { lbl, line });
            return y + 42;
        }

        private void AddFieldLabel(Panel parent, string text, int left, int top)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = GetFont(9, FontStyle.Regular),
                ForeColor = Muted,
                AutoSize = true,
                Left = left,
                Top = top,
                BackColor = Color.Transparent
            });
        }

        private CheckBox AddToggle(Panel parent, string text, bool isChecked, int y)
        {
            var cb = new CheckBox
            {
                Text = text,
                Font = GetFont(10, FontStyle.Regular),
                // FIX: ForeColor más visible — Muted era muy oscuro para checkboxes
                ForeColor = Color.FromArgb(200, 200, 210),
                BackColor = Color.Transparent,
                Checked = isChecked,
                AutoSize = true,
                Left = 0,
                Top = y,
                Cursor = Cursors.Hand
            };
            parent.Controls.Add(cb);
            return cb;
        }

        private ComboBox MakeCombo(Panel parent, string[] items,
            string selected, int left, int top, int width)
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
                Width = width,
                // FIX: Height fijo consistente con los demás inputs
                Height = 34
            };
            cb.Items.AddRange(items);
            cb.SelectedItem = items.Contains(selected) ? selected : items[0];
            parent.Controls.Add(cb);
            return cb;
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

        private Button MakeRedButton(string text, int left, int top, int width, int height)
        {
            var btn = new Button
            {
                Text = text,
                Font = GetFont(9, FontStyle.Bold),
                ForeColor = White,
                BackColor = Red,
                FlatStyle = FlatStyle.Flat,
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = RedLight;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 219, 41, 85);
            DrawHelper.MakeRounded(btn, 8);
            return btn;
        }

        private static Font GetFont(float size, FontStyle style) =>
            MainForm.GetFont(size, style);
    }
}