namespace OrbixaDownloader.Forms
{
    partial class SplashForm
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
            this.ClientSize = new Size(520, 320);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = OrbixaDownloader.Forms.DrawHelper.Canvas;
            this.Name = "SplashForm";
            this.Text = "Orbixa Downloader";
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;

            // Click anywhere to skip update wait (safety valve)
            this.MouseClick += (_, _) => { /* no-op, update runs in background */ };

            this.ResumeLayout(false);
        }
    }
}