using OrbixaDownloader.Models;
using OrbixaDownloader.Services;

namespace OrbixaDownloader.Forms
{
    public class SettingsForm : Form
    {
        private readonly AppSettings _settings;
        private readonly UpdaterService _updater;

        private static Color BgDeep => DrawHelper.Canvas;
        private static Color BgCard => DrawHelper.Glass;
        private static Color Red => DrawHelper.Accent;
        private static Color RedLight => DrawHelper.SurfaceStrong;
        private static Color White => DrawHelper.Text;
        private static Color Muted => DrawHelper.Muted;
        private static Color Surface => DrawHelper.Field;

        // FIX: Ancho interno real del scroll = 560 - 2*28 padding = 504px
        // Todos los controles internos deben usar este ancho como referencia
        private const int InnerWidth = 504;

        private bool _dragging;
        private Point _dragStart;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;
            _settings.Normalize();
            DrawHelper.SetTheme(_settings.Theme, _settings.CustomTheme);
            _updater = new UpdaterService(settings);
            InitForm();
            BuildUI();
            UiText.Apply(this);
            DrawHelper.ApplyTheme(this);
        }

        private void InitForm()
        {
            Text = UiText.Get("Ajustes — Orbixa", "Orbixa Settings");
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
                using var pen = new Pen(DrawHelper.Border, 1f);
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

            y = AddSectionHeader(scroll, UiText.Get("Apariencia", "Appearance"), y);
            AddFieldLabel(scroll, UiText.Get("Tema:", "Theme:"), 0, y + 7);
            var themeCombo = MakeCombo(scroll,
                Enum.GetValues<ThemePreset>().Where(preset => preset is not ThemePreset.SystemHighContrast and not ThemePreset.Personalizado).Select(ThemeName).Append(ThemeName(ThemePreset.SystemHighContrast)).Append(ThemeName(ThemePreset.Personalizado)).ToArray(),
                ThemeName(_settings.Theme), 82, y, 250);
            var customSelectors = new[]
            {
                MakeCustomCombo(scroll, UiText.Get("Fondo", "Canvas"), _settings.CustomTheme.Canvas, 0, y + 48),
                MakeCustomCombo(scroll, UiText.Get("Superficie", "Surface"), _settings.CustomTheme.Surface, 168, y + 48),
                MakeCustomCombo(scroll, UiText.Get("Campo", "Field"), _settings.CustomTheme.Field, 336, y + 48),
                MakeCustomCombo(scroll, UiText.Get("Acento primario", "Primary accent"), _settings.CustomTheme.Primary, 0, y + 96),
                MakeCustomCombo(scroll, UiText.Get("Acento secundario", "Secondary accent"), _settings.CustomTheme.Secondary, 168, y + 96),
                MakeCustomCombo(scroll, UiText.Get("Foco", "Focus"), _settings.CustomTheme.Focus, 336, y + 96)
            };
            var preview = new Panel { Left = 0, Top = y + 144, Width = InnerWidth, Height = 34, BackColor = DrawHelper.Field, AccessibleName = UiText.Get("Vista previa del tema", "Theme preview") };
            preview.Controls.Add(new Label { Text = UiText.Get("Vista previa: texto normal y grande con contraste WCAG", "Preview: normal and large text with WCAG contrast"), AutoSize = true, Left = 10, Top = 8, ForeColor = DrawHelper.Text, BackColor = Color.Transparent });
            scroll.Controls.Add(preview);
            customSelectors.ToList().ForEach(combo => combo.Visible = _settings.Theme == ThemePreset.Personalizado);
            preview.Visible = _settings.Theme == ThemePreset.Personalizado;
            bool changingCustom = false;
            CustomThemeSettings lastValidCustom = CloneCustomTheme(_settings.CustomTheme);
            themeCombo.SelectedIndexChanged += (_, _) =>
            {
                var selectablePresets = Enum.GetValues<ThemePreset>().Where(preset => preset is not ThemePreset.SystemHighContrast and not ThemePreset.Personalizado).ToArray();
                var selected = themeCombo.SelectedIndex < selectablePresets.Length
                    ? selectablePresets[themeCombo.SelectedIndex]
                    : themeCombo.SelectedIndex == selectablePresets.Length ? ThemePreset.SystemHighContrast : ThemePreset.Personalizado;
                if (!DrawHelper.SetTheme(selected, _settings.CustomTheme))
                {
                    MessageBox.Show(this, UiText.Get("La combinación de colores no tiene contraste suficiente.", "This color combination does not have sufficient contrast."),
                        UiText.Get("Tema no válido", "Invalid theme"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    themeCombo.SelectedItem = ThemeName(_settings.Theme);
                    return;
                }
                _settings.Theme = selected;
                _settings.Save();
                customSelectors.ToList().ForEach(combo => combo.Visible = selected == ThemePreset.Personalizado);
                preview.Visible = selected == ThemePreset.Personalizado;
                DrawHelper.ApplyTheme(this);
                BackColor = DrawHelper.Canvas;
            };
            foreach (var combo in customSelectors)
                combo.SelectedIndexChanged += (_, _) =>
                {
                    if (changingCustom) return;
                    _settings.CustomTheme = ReadCustomTheme(customSelectors);
                    if (!DrawHelper.SetTheme(ThemePreset.Personalizado, _settings.CustomTheme))
                    {
                        MessageBox.Show(this, UiText.Get("La combinación no cumple el contraste WCAG. Se conserva el último tema válido.", "This combination does not meet WCAG contrast. The last valid theme is kept."), UiText.Get("Tema no válido", "Invalid theme"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _settings.CustomTheme = CloneCustomTheme(lastValidCustom);
                        changingCustom = true;
                        for (int i = 0; i < customSelectors.Length; i++) customSelectors[i].SelectedIndex = (int)new[] { lastValidCustom.Canvas, lastValidCustom.Surface, lastValidCustom.Field, lastValidCustom.Primary, lastValidCustom.Secondary, lastValidCustom.Focus }[i];
                        changingCustom = false;
                        return;
                    }
                    lastValidCustom = CloneCustomTheme(_settings.CustomTheme);
                    _settings.Theme = ThemePreset.Personalizado;
                    _settings.Save();
                    DrawHelper.ApplyTheme(this);
                    preview.BackColor = DrawHelper.Field;
                };
            AddFieldLabel(scroll, UiText.Get("Idioma:", "Language:"), 0, y + (_settings.Theme == ThemePreset.Personalizado ? 194 : 49));
            var languageCombo = MakeCombo(scroll, new[] { "Español", "English" },
                _settings.Language == UiLanguage.English ? "English" : "Español", 82, y + (_settings.Theme == ThemePreset.Personalizado ? 187 : 42), 150);
            languageCombo.SelectedIndexChanged += (_, _) =>
            {
                _settings.Language = languageCombo.SelectedIndex == 1 ? UiLanguage.English : UiLanguage.Espanol;
                _settings.Save();
                MessageBox.Show(this,
                    UiText.Get("El idioma se aplicará a las ventanas nuevas. La ventana principal se actualizará al cerrar Ajustes. Si el reproductor está abierto, cerralo y volvé a abrirlo.", "The language applies to new windows. The main window updates when Settings closes. If the player is open, close and reopen it."),
                    UiText.Get("Idioma actualizado", "Language updated"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            y += _settings.Theme == ThemePreset.Personalizado ? 194 : 96;

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
                ReadOnly = true,
                AccessibleName = "Carpeta de destino",
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
                    Description = UiText.Get("Seleccioná la carpeta de destino", "Select the destination folder"),
                    SelectedPath = _settings.DefaultOutputFolder,
                    UseDescriptionForTitle = true
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try { DownloadService.ValidateOutputFolder(dlg.SelectedPath); }
                    catch (Exception ex) { ErrorDetailsDialog.ShowDetails(this, ex.Message); return; }
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

            AddFieldLabel(scroll, "Simultáneas (al reiniciar):", 0, y + 7);
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
                Text = string.IsNullOrWhiteSpace(_settings.InstalledYtDlpVersion) ? "yt-dlp: No instalado" : $"yt-dlp: {_settings.InstalledYtDlpVersion}",
                Font = GetFont(9, FontStyle.Regular),
                ForeColor = DrawHelper.Positive,
                AutoSize = true,
                Left = 0,
                Top = y,
                BackColor = Color.Transparent
            };
            scroll.Controls.Add(verLabel);
            y += 34;

            var updateNowBtn = MakeRedButton(UiText.Get("Actualizar ahora", "Update now"), 0, y, 190, 36);
            var updateStatus = new Label { Left = 0, Top = y + 44, Width = InnerWidth, Height = 48, ForeColor = Muted, Text = UiText.Get("Verifica yt-dlp, FFmpeg, FFprobe y Deno.", "Checks yt-dlp, FFmpeg, FFprobe and Deno.") };
            string updateDetails = "";
            var detailsBtn = MakeRedButton("Ver detalles", 204, y, 140, 36);
            detailsBtn.Visible = false;
            detailsBtn.Click += (_, _) => ErrorDetailsDialog.ShowDetails(this, updateDetails);
            _updater.ProgressChanged += (_, progress) => { if (!IsDisposed) updateStatus.Text = UiText.LocalizeProgress(progress.Message); };
            updateNowBtn.Click += async (_, _) =>
            {
                updateNowBtn.Enabled = false;
                detailsBtn.Visible = false;
                updateNowBtn.Text = "Actualizando...";
                try
                {
                    await _updater.CheckAndUpdateAllAsync();
                    verLabel.Text = $"yt-dlp: {_settings.InstalledYtDlpVersion}";
                    updateStatus.Text = UiText.Get("Herramientas instaladas y verificadas. Ya podés descargar.", "Tools installed and verified. You can download now.");
                }
                catch (Exception ex)
                {
                    updateDetails = ex.Message;
                    updateStatus.Text = UiText.Get("No se completó la actualización. Consultá los detalles y reintentá.", "The update did not complete. Check the details and try again.");
                    detailsBtn.Visible = true;
                }
                finally { updateNowBtn.Enabled = true; updateNowBtn.Text = UiText.Get("Actualizar ahora", "Update now"); }
            };
            scroll.Controls.AddRange(new Control[] { updateNowBtn, detailsBtn, updateStatus });
            y += 104;
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
                BackColor = DrawHelper.Border,
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
                ForeColor = DrawHelper.Text,
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

        private ComboBox MakeCustomCombo(Panel parent, string label, CuratedColor selected, int left, int top)
        {
            AddFieldLabel(parent, label, left, top);
            return MakeCombo(parent, Enum.GetValues<CuratedColor>().Select(CuratedColorName).ToArray(), CuratedColorName(selected), left, top + 17, 150);
        }

        private static CustomThemeSettings ReadCustomTheme(ComboBox[] combos)
        {
            CuratedColor Read(ComboBox combo) => Enum.IsDefined(typeof(CuratedColor), combo.SelectedIndex) ? (CuratedColor)combo.SelectedIndex : CuratedColor.Mineral;
            return new CustomThemeSettings { Canvas = Read(combos[0]), Surface = Read(combos[1]), Field = Read(combos[2]), Primary = Read(combos[3]), Secondary = Read(combos[4]), Focus = Read(combos[5]) };
        }

        private static CustomThemeSettings CloneCustomTheme(CustomThemeSettings source) => new()
        {
            Canvas = source.Canvas,
            Surface = source.Surface,
            Field = source.Field,
            Primary = source.Primary,
            Secondary = source.Secondary,
            Focus = source.Focus
        };

        private static string CuratedColorName(CuratedColor color) => color switch
        {
            CuratedColor.Turquesa => UiText.Get("Turquesa", "Turquoise"),
            CuratedColor.Coral => UiText.Get("Coral", "Coral"),
            CuratedColor.Cielo => UiText.Get("Cielo", "Sky"),
            CuratedColor.Grafito => UiText.Get("Grafito", "Graphite"),
            _ => UiText.Get("Mineral", "Mineral")
        };

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
            btn.FlatAppearance.MouseDownBackColor = DrawHelper.PrimaryDark;
            DrawHelper.MakeRounded(btn, 8);
            return btn;
        }

        private static Font GetFont(float size, FontStyle style) =>
            MainForm.GetFont(size, style);

        private static string ThemeName(ThemePreset preset) => preset switch
        {
            ThemePreset.CoralNocturno => UiText.Get("Coral Nocturno", "Night Coral"),
            ThemePreset.TurquesaNocturno => UiText.Get("Turquesa Nocturno", "Night Turquoise"),
            ThemePreset.CieloArtico => UiText.Get("Cielo Ártico", "Arctic Sky"),
            ThemePreset.GrafitoSuave => UiText.Get("Grafito Suave", "Soft Graphite"),
            ThemePreset.AmbarNocturno => UiText.Get("Ámbar Nocturno", "Night Amber"),
            ThemePreset.MineralTurquesa => UiText.Get("Mineral Turquesa", "Mineral Turquoise"),
            ThemePreset.MineralCoral => UiText.Get("Mineral Coral", "Mineral Coral"),
            ThemePreset.CieloCobalto => UiText.Get("Cielo Cobalto", "Cobalt Sky"),
            ThemePreset.OlivaLimon => UiText.Get("Oliva Limón", "Lime Olive"),
            ThemePreset.AmapolaAzul => UiText.Get("Amapola Azul", "Blue Poppy"),
            ThemePreset.ArcillaCielo => UiText.Get("Arcilla Cielo", "Sky Clay"),
            ThemePreset.ObsidianaAmbar => UiText.Get("Obsidiana Ámbar", "Amber Obsidian"),
            ThemePreset.MarfilGrafito => UiText.Get("Marfil Grafito", "Graphite Ivory"),
            ThemePreset.BosqueCobre => UiText.Get("Bosque Cobre", "Copper Forest"),
            ThemePreset.AzulTintaMandarina => UiText.Get("Azul Tinta Mandarina", "Ink Blue Tangerine"),
            ThemePreset.PizarraLima => UiText.Get("Pizarra Lima", "Lime Slate"),
            ThemePreset.OrbixaElevated => UiText.Get("Orbixa Elevado", "Orbixa Elevated"),
            ThemePreset.OrbixaQuiet => UiText.Get("Orbixa Quieto", "Orbixa Quiet"),
            ThemePreset.OrbixaNight => UiText.Get("Orbixa Noche", "Orbixa Night"),
            ThemePreset.GraphiteCalm => UiText.Get("Grafito Calmado", "Graphite Calm"),
            ThemePreset.OceanGlass => UiText.Get("Cristal Oceánico", "Ocean Glass"),
            ThemePreset.SunsetGlass => UiText.Get("Cristal Atardecer", "Sunset Glass"),
            ThemePreset.SystemHighContrast => UiText.Get("Alto contraste del sistema", "System high contrast"),
            ThemePreset.Personalizado => UiText.Get("Personalizado", "Custom"),
            _ => UiText.Get("Orbixa Mineral", "Orbixa Mineral")
        };
    }
}
