using OrbixaDownloader.Models;
using System.ComponentModel;

namespace OrbixaDownloader.Forms;

public class DownloadCard : Panel
{
    private readonly DownloadItem _item;
    private readonly Label _title = new() { AutoEllipsis = true, Dock = DockStyle.Fill, ForeColor = DrawHelper.Text };
    private readonly Label _status = new() { Dock = DockStyle.Fill, ForeColor = DrawHelper.Muted };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Maximum = 100 };
    private readonly Button _cancel = new() { AutoSize = true };
    private readonly Button _retry = new() { AutoSize = true };
    private readonly Button _details = new() { AutoSize = true };
    private readonly Button _folder = new() { AutoSize = true };
    private readonly System.Windows.Forms.Timer _timer;

    public DownloadCard(DownloadItem item, Action<DownloadItem> onCancel, Action<DownloadItem> onOpenFolder, Action<DownloadItem>? onRetry = null)
    {
        _item = item;
        Height = 124;
        Margin = new Padding(0, 0, 0, 8);
        Padding = new Padding(12, 8, 12, 8);
        BackColor = MainForm.BgCard;
        Font = MainForm.GetFont(9, FontStyle.Regular);
        AccessibleName = UiText.Get("Descarga", "Download");
        _cancel.Text = UiText.Get("Cancelar", "Cancel");
        _retry.Text = UiText.Get("Reintentar", "Retry");
        _details.Text = UiText.Get("Ver detalles", "View details");
        _folder.Text = UiText.Get("Abrir carpeta", "Open folder");
        _cancel.AccessibleName = UiText.Get("Cancelar descarga", "Cancel download");
        _retry.AccessibleName = UiText.Get("Reintentar descarga", "Retry download");
        _details.AccessibleName = UiText.Get("Ver detalles del error", "View error details");
        _folder.AccessibleName = UiText.Get("Abrir carpeta de descarga", "Open download folder");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(_title, 0, 0); layout.Controls.Add(_status, 0, 1); layout.Controls.Add(_progress, 0, 2);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        foreach (var button in new[] { _cancel, _retry, _details, _folder })
        {
            button.BackColor = MainForm.Surface;
            button.ForeColor = DrawHelper.Text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = DrawHelper.Border;
            button.Height = 28;
            button.Padding = new Padding(8, 0, 8, 0);
            button.Margin = new Padding(0, 0, 8, 0);
            DrawHelper.MakeRounded(button, 8);
            actions.Controls.Add(button);
        }
        layout.Controls.Add(actions, 0, 3); Controls.Add(layout);
        _cancel.Click += (_, _) => onCancel(_item);
        _retry.Click += (_, _) => onRetry?.Invoke(_item);
        _details.Click += (_, _) => ErrorDetailsDialog.ShowDetails(this, _item.ErrorDetails);
        _folder.Click += (_, _) => onOpenFolder(_item);
        DrawHelper.ApplyTheme(this);
        _timer = new System.Windows.Forms.Timer { Interval = 150 };
        _timer.Tick += (_, _) => RefreshItem();
        _timer.Start(); RefreshItem();
    }

    private void RefreshItem()
    {
        _title.Text = _item.Title;
        _status.Text = StatusLabel(_item.Status, _item.Progress);
        _status.AccessibleName = UiText.StatusAccessible(_item.Status, _item.Progress);
        _progress.Value = Math.Clamp((int)_item.Progress, 0, 100);
        bool active = _item.Status is DownloadStatus.Pending or DownloadStatus.FetchingInfo or DownloadStatus.Downloading or DownloadStatus.Converting;
        _cancel.Visible = active;
        _retry.Visible = _item.Status is DownloadStatus.Failed or DownloadStatus.Cancelled;
        _details.Visible = _item.Status == DownloadStatus.Failed;
        _folder.Visible = _item.Status == DownloadStatus.Completed;
        _progress.Visible = active;
    }

    private static string StatusLabel(DownloadStatus status, double progress)
    {
        string marker = status switch
        {
            DownloadStatus.Pending => "[ ]",
            DownloadStatus.FetchingInfo => "[i]",
            DownloadStatus.Downloading => "[v]",
            DownloadStatus.Converting => "[~]",
            DownloadStatus.Completed => "[+]",
            DownloadStatus.Failed => "[!]",
            DownloadStatus.Cancelled => "[-]",
            _ => "[ ]"
        };
        return marker + " " + UiText.StatusText(status, progress);
    }

    protected override void OnPaintBackground(PaintEventArgs e) => DrawHelper.PaintGlass(e.Graphics, ClientRectangle);

    protected override void Dispose(bool disposing) { if (disposing) _timer.Dispose(); base.Dispose(disposing); }
}

internal static class ErrorDetailsDialog
{
    public static void ShowDetails(IWin32Window owner, string details)
    {
        if (string.IsNullOrWhiteSpace(details)) details = UiText.Get("No hay detalles adicionales. Verificá el enlace y actualizá las herramientas en Ajustes.", "No additional details. Check the link and update the tools in Settings.");
        using var dialog = new Form { Text = UiText.Get("Detalles del error", "Error details"), Size = new Size(720, 440), MinimumSize = new Size(420, 280), StartPosition = FormStartPosition.CenterParent };
        var text = new TextBox { Text = details, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, AccessibleName = UiText.Get("Detalles completos del error", "Full error details") };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(6) };
        var close = new Button { Text = UiText.Get("Cerrar", "Close"), DialogResult = DialogResult.Cancel, AutoSize = true };
        var copy = new Button { Text = UiText.Get("Copiar detalles", "Copy details"), AutoSize = true };
        copy.Click += (_, _) => { try { Clipboard.SetText(text.Text); } catch (System.Runtime.InteropServices.ExternalException) { MessageBox.Show(dialog, UiText.Get("El portapapeles está ocupado. Volvé a intentar.", "The clipboard is busy. Try again.")); } };
        text.KeyDown += (_, e) => { if (e.Control && e.KeyCode == Keys.A) { text.SelectAll(); e.SuppressKeyPress = true; } };
        buttons.Controls.Add(close); buttons.Controls.Add(copy);
        dialog.Controls.Add(text); dialog.Controls.Add(buttons); dialog.CancelButton = close;
        DrawHelper.ApplyTheme(dialog);
        dialog.ShowDialog(owner);
    }
}

