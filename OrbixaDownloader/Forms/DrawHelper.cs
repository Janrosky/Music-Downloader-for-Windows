using System.Drawing.Drawing2D;

namespace OrbixaDownloader.Forms
{
    /// <summary>
    /// Utilidades de dibujo reutilizables para toda la UI.
    /// </summary>
    public static class DrawHelper
    {
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
            control.Paint += (s, e) =>
            {
                var c = (Control)s!;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            };

            // Re-apply on resize
            control.Resize += (s, _) => ApplyRoundedRegion((Control)s!, radius);
            ApplyRoundedRegion(control, radius);
        }

        private static void ApplyRoundedRegion(Control c, int radius)
        {
            if (c.Width <= 0 || c.Height <= 0) return;
            using var path = RoundedPath(new Rectangle(0, 0, c.Width, c.Height), radius);
            c.Region = new Region(path);
        }

        /// <summary>
        /// Configura apariencia visual para un Label usado como botón de nav.
        /// </summary>
        public static void AddRoundedAppearance(Label label, int radius)
        {
            label.Resize += (s, _) =>
            {
                var l = (Label)s!;
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

        public static string Truncate(Graphics g, string text, Font font, float maxWidth)
        {
            if (g.MeasureString(text, font).Width <= maxWidth) return text;
            while (text.Length > 3 && g.MeasureString(text + "…", font).Width > maxWidth)
                text = text[..^1];
            return text + "…";
        }
    }
}