using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using OrbixaDownloader.Models;

namespace OrbixaDownloader.Forms
{
    /// <summary>
    /// Utilidades de dibujo reutilizables para toda la UI.
    /// </summary>
    public static class DrawHelper
    {
        public const string PreserveSurfaceTag = "orbixa-preserve-surface";
        private static ThemePreset _preset = ThemePreset.OrbixaMineral;
        private static CustomThemeSettings _customTheme = new();
        private static readonly ConditionalWeakTable<Button, ButtonStyleState> ButtonStates = new();
        private static readonly ConditionalWeakTable<Control, RoundedState> RoundedStates = new();
        private static ThemePalette Palette => OrbixaThemes.Resolve(_preset, _customTheme);
        public static Color Canvas => Palette.Canvas;
        public static Color RaisedCanvas => Palette.RaisedCanvas;
        public static Color Glass => Palette.Surface;
        public static Color SurfaceStrong => Palette.SurfaceStrong;
        public static Color SurfaceSolid => Palette.SurfaceSolid;
        public static Color Field => Palette.Field;
        public static Color Text => Palette.Text;
        public static Color Muted => Palette.Muted;
        public static Color Accent => Palette.Primary;
        public static Color Secondary => Palette.Secondary;
        public static Color Focus => Palette.Focus;
        public static Color Border => Palette.Border;
        public static Color BorderHighlight => Palette.BorderHighlight;
        public static Color PrimaryDark => Palette.PrimaryDark;
        public static Color Success => Palette.Success;
        public static Color Positive => Palette.Success;
        public static Color Warning => Palette.Warning;
        public static Color Error => Palette.Error;

        public static ThemePreset CurrentPreset => SystemInformation.HighContrast ? ThemePreset.SystemHighContrast : _preset;
        private static bool AnimationsEnabled => !SystemInformation.HighContrast && SystemInformation.IsMenuAnimationEnabled;

        public static bool SetTheme(ThemePreset preset, CustomThemeSettings? custom = null)
        {
            if (!OrbixaThemes.IsValid(preset, custom)) return false;
            _preset = preset;
            if (custom != null) _customTheme = custom;
            return true;
        }

        public static void PaintGlass(Graphics g, Rectangle bounds, int radius = 18)
        {
            if (bounds.Width < 4 || bounds.Height < 4) return;
            bounds.Inflate(-1, -1);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (SystemInformation.HighContrast) { FillRounded(g, bounds, radius, SystemColors.Control); DrawRounded(g, bounds, radius, SystemColors.WindowText); return; }
            using var gradient = new LinearGradientBrush(bounds, SurfaceStrong, Glass, 90f);
            FillRounded(g, bounds, radius, gradient);
            DrawRounded(g, bounds, radius, Border);
            using var pen = new Pen(BorderHighlight);
            g.DrawLine(pen, bounds.Left + radius, bounds.Top + 1, bounds.Right - radius, bounds.Top + 1);
        }

        public static void ApplyTheme(Control root)
        {
            root.BackColor = root is Form ? Canvas : root.BackColor;
            foreach (Control control in root.Controls)
            {
                if (control is TextBox or ComboBox) { control.BackColor = Field; control.ForeColor = Text; }
                else if (control is Button button)
                {
                    ApplyButtonTheme(button);
                    button.AccessibleName ??= button.Text;
                    MakeRounded(button, 12);
                }
                else if (control is Label or CheckBox) control.ForeColor = Text;
                else if (control is Panel && control is not FlowLayoutPanel && control is not TableLayoutPanel
                    && !Equals(control.Tag, PreserveSurfaceTag))
                {
                    control.BackColor = Glass;
                }
                if (control is Form) control.BackColor = Canvas;
                ApplyTheme(control);
            }
        }

        private static void ApplyButtonTheme(Button button)
        {
            var state = ButtonStates.GetValue(button, CreateButtonState);
            if (!state.Initialized)
            {
                state.UsesThemeSurface = button.BackColor == Color.Transparent || button.BackColor.A == 0 || button.BackColor == SystemColors.Control;
                state.UsesAccent = button.BackColor == Accent;
                state.BaseBackColor = state.UsesThemeSurface ? Glass : button.BackColor;
                state.Initialized = true;
            }
            else if (state.UsesThemeSurface || state.UsesAccent)
                state.BaseBackColor = state.UsesThemeSurface ? Glass : Accent;

            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = SystemInformation.HighContrast ? SystemColors.WindowText : Border;
            button.FlatAppearance.MouseOverBackColor = SystemInformation.HighContrast ? SystemColors.Highlight : SurfaceStrong;
            button.FlatAppearance.MouseDownBackColor = SystemInformation.HighContrast
                ? SystemColors.Highlight
                : state.UsesAccent ? Blend(Accent, Color.White, .08) : Blend(SurfaceStrong, Color.White, .06);
            button.BackColor = state.BaseBackColor;
            button.ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : TextColorFor(state.BaseBackColor);
            button.Invalidate();
        }

        private static ButtonStyleState CreateButtonState(Button button)
        {
            var state = new ButtonStyleState();
            button.MouseEnter += OnButtonMouseEnter;
            button.MouseLeave += OnButtonMouseLeave;
            button.MouseDown += OnButtonMouseDown;
            button.MouseUp += OnButtonMouseUp;
            button.EnabledChanged += OnButtonEnabledChanged;
            button.Paint += OnButtonPaint;
            button.Disposed += OnButtonDisposed;
            return state;
        }

        private static void OnButtonMouseEnter(object? sender, EventArgs e)
        {
            if (sender is not Button button || !button.Enabled || SystemInformation.HighContrast) return;
            var state = ButtonStates.GetValue(button, CreateButtonState);
            state.IsHovered = true;
            TransitionButton(button, HoverSurface(state.BaseBackColor));
        }

        private static void OnButtonMouseLeave(object? sender, EventArgs e)
        {
            if (sender is not Button button) return;
            var state = ButtonStates.GetValue(button, CreateButtonState);
            state.IsHovered = false;
            TransitionButton(button, state.BaseBackColor);
        }

        private static void OnButtonMouseDown(object? sender, MouseEventArgs e)
        {
            if (sender is not Button button || !button.Enabled || e.Button != MouseButtons.Left || SystemInformation.HighContrast) return;
            var state = ButtonStates.GetValue(button, CreateButtonState);
            StopButtonTransition(state);
            state.IsPressed = true;
            button.BackColor = state.UsesAccent
                ? Blend(state.BaseBackColor, Color.White, .08)
                : Blend(state.BaseBackColor, Color.White, .14);
            button.Invalidate();
        }

        private static void OnButtonMouseUp(object? sender, MouseEventArgs e)
        {
            if (sender is not Button button || !button.Enabled) return;
            if (ButtonStates.TryGetValue(button, out var state)) state.IsPressed = false;
            OnButtonMouseEnter(button, EventArgs.Empty);
        }

        private static void OnButtonEnabledChanged(object? sender, EventArgs e)
        {
            if (sender is not Button button) return;
            var state = ButtonStates.GetValue(button, CreateButtonState);
            button.BackColor = button.Enabled ? state.BaseBackColor : Blend(state.BaseBackColor, Muted, 0.35);
            button.ForeColor = button.Enabled ? TextColorFor(state.BaseBackColor) : Muted;
            button.Invalidate();
        }

        private static void OnButtonPaint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button button || SystemInformation.HighContrast) return;
            if (button.Focused)
            {
                using var pen = new Pen(Focus, 2f);
                var bounds = Rectangle.Inflate(button.ClientRectangle, -2, -2);
                using var path = RoundedPath(bounds, Math.Min(10, Math.Max(2, bounds.Height / 4)));
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(pen, path);
            }
            if (button.Enabled && ButtonStates.TryGetValue(button, out var state) && state.IsHovered)
            {
                using var highlight = new Pen(BorderHighlight);
                e.Graphics.DrawLine(highlight, 6, 2, Math.Max(6, button.Width - 6), 2);
                DrawGlow(e.Graphics, button.Width / 2, button.Height / 2, Math.Max(8, button.Height / 2), Color.FromArgb(18, Accent));
            }
        }

        private static void OnButtonDisposed(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                var state = ButtonStates.GetValue(button, CreateButtonState);
                StopButtonTransition(state);
                ButtonStates.Remove(button);
            }
        }

        private static void TransitionButton(Button button, Color target)
        {
            if (!AnimationsEnabled)
            {
                button.BackColor = target;
                button.Invalidate();
                return;
            }

            var state = ButtonStates.GetValue(button, CreateButtonState);
            StopButtonTransition(state);
            Color start = button.BackColor;
            int elapsed = 0;
            var timer = new System.Windows.Forms.Timer { Interval = 15 };
            state.Transition = timer;
            timer.Tick += (_, _) =>
            {
                elapsed += timer.Interval;
                double progress = Math.Min(1, elapsed / 120d);
                button.BackColor = Blend(start, target, progress);
                button.Invalidate();
                if (progress >= 1) StopButtonTransition(state);
            };
            timer.Start();
        }

        private static void StopButtonTransition(ButtonStyleState state)
        {
            state.Transition?.Stop();
            state.Transition?.Dispose();
            state.Transition = null;
        }

        private static Color TextColorFor(Color background)
            => ThemePalette.ContrastRatio(Text, background) >= 4.5 ? Text
                : ThemePalette.ContrastRatio(Canvas, background) >= 4.5 ? Canvas : Color.Black;

        private static Color HoverSurface(Color background)
            => Blend(background, Color.White, .08);

        private static Color Blend(Color baseColor, Color accent, double amount)
            => Color.FromArgb(
                (int)Math.Round(baseColor.R + (accent.R - baseColor.R) * amount),
                (int)Math.Round(baseColor.G + (accent.G - baseColor.G) * amount),
                (int)Math.Round(baseColor.B + (accent.B - baseColor.B) * amount));

        // ── Rounded fill ─────────────────────────────────────────────────────

        public static void FillRounded(Graphics g, Rectangle r, int radius, Color color)
        {
            using var brush = new SolidBrush(color);
            using var path = RoundedPath(r, radius);
            g.FillPath(brush, path);
        }

        public static void FillRounded(Graphics g, Rectangle r, int radius, Brush brush)
        {
            using var path = RoundedPath(r, radius);
            g.FillPath(brush, path);
        }

        // ── Rounded stroke ────────────────────────────────────────────────────

        public static void DrawRounded(Graphics g, Rectangle r, int radius, Color color, float strokeWidth = 1f)
        {
            using var pen = new Pen(color, strokeWidth);
            using var path = RoundedPath(r, radius);
            g.DrawPath(pen, path);
        }

        // ── Radial glow ───────────────────────────────────────────────────────

        public static void DrawGlow(Graphics g, int cx, int cy, int radius, Color color)
        {
            using var path = new GraphicsPath();
            path.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
            using var brush = new PathGradientBrush(path)
            {
                CenterColor = color,
                SurroundColors = new[] { Color.Transparent }
            };
            g.FillPath(brush, path);
        }

        // ── Path builder ──────────────────────────────────────────────────────

        public static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ── Control helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Hace que un control tenga bordes redondeados usando Region.
        /// Útil para Buttons y Panels simples.
        /// </summary>
        public static void MakeRounded(Control control, int radius)
        {
            if (RoundedStates.TryGetValue(control, out _)) return;
            RoundedStates.Add(control, new RoundedState(radius));
            control.Resize += OnRoundedResize;
            control.Disposed += OnRoundedDisposed;
            ApplyRoundedRegion(control, radius);
        }

        private static void OnRoundedResize(object? sender, EventArgs e)
        {
            if (sender is Control control && RoundedStates.TryGetValue(control, out var state))
                ApplyRoundedRegion(control, state.Radius);
        }

        private static void OnRoundedDisposed(object? sender, EventArgs e)
        {
            if (sender is Control control) RoundedStates.Remove(control);
        }

        private static void ApplyRoundedRegion(Control c, int radius)
        {
            if (c.Width <= 0 || c.Height <= 0) return;
            using var path = RoundedPath(new Rectangle(0, 0, c.Width, c.Height), radius);
            var oldRegion = c.Region;
            c.Region = new Region(path);
            oldRegion?.Dispose();
        }

        /// <summary>
        /// Configura apariencia visual para un Label usado como botón de nav.
        /// </summary>
        public static void AddRoundedAppearance(Control label, int radius)
        {
            label.Resize += (s, _) =>
            {
                var l = (Control)s!;
                if (l.Width > 0 && l.Height > 0)
                {
                    using var path = RoundedPath(new Rectangle(0, 0, l.Width, l.Height), radius);
                    l.Region = new Region(path);
                }
            };
        }

        // ── Text helpers ──────────────────────────────────────────────────────

        public static void DrawCenteredText(Graphics g, string text, Font font,
            Color color, Rectangle bounds)
        {
            using var brush = new SolidBrush(color);
            var size = g.MeasureString(text, font);
            float x = bounds.X + (bounds.Width - size.Width) / 2f;
            float y = bounds.Y + (bounds.Height - size.Height) / 2f;
            g.DrawString(text, font, brush, x, y);
        }

        public static void StyleComboBox(ComboBox combo)
        {
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = 26;
            combo.DrawItem += (_, e) =>
            {
                if (e.Index < 0) return;
                bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color background = selected ? SurfaceStrong : Field;
                using var brush = new SolidBrush(background);
                e.Graphics.FillRectangle(brush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, combo.GetItemText(combo.Items[e.Index]), combo.Font,
                    Rectangle.Inflate(e.Bounds, -8, 0), Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
        }

        public static string Truncate(Graphics g, string text, Font font, float maxWidth)
        {
            if (g.MeasureString(text, font).Width <= maxWidth) return text;
            while (text.Length > 3 && g.MeasureString(text + "…", font).Width > maxWidth)
                text = text[..^1];
            return text + "…";
        }

        private sealed class ButtonStyleState
        {
            public bool Initialized { get; set; }
            public bool UsesThemeSurface { get; set; }
            public bool UsesAccent { get; set; }
            public bool IsHovered { get; set; }
            public bool IsPressed { get; set; }
            public Color BaseBackColor { get; set; }
            public System.Windows.Forms.Timer? Transition { get; set; }
        }

        private sealed class RoundedState
        {
            public RoundedState(int radius) => Radius = radius;
            public int Radius { get; }
        }
    }
}

