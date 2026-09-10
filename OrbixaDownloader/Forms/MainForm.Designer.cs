namespace OrbixaDownloader.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.ClientSize = new Size(980, 660);
            this.MinimumSize = new Size(820, 560);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = OrbixaDownloader.Forms.DrawHelper.Canvas;
            this.DoubleBuffered = true;
            this.Name = "MainForm";
            this.Text = "Orbixa Downloader";

            // Allow resize via bottom-right corner
            this.ResizeRedraw = true;

            // Drag-to-resize grip (bottom-right)
            this.MouseDown += (s, e) =>
            {
                if (e.X > Width - 16 && e.Y > Height - 16)
                    ResizeWindow();
            };

            this.ResumeLayout(false);
        }

        private void ResizeWindow()
        {
            // Native resize via WM_NCLBUTTONDOWN + HTBOTTOMRIGHT
            ReleaseCapture();
            SendMessage(Handle, 0x112, 0xF008, 0);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    }
}