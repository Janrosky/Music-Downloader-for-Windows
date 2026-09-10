using OrbixaDownloader.Models;

namespace OrbixaDownloader.Forms;

public static class UiText
{
    public static UiLanguage Language { get; set; } = UiLanguage.Espanol;

    public static string Get(string spanish, string english) => Language == UiLanguage.English ? english : spanish;


    public static string Localize(string value) => value switch
    {
        "⬇  Descargar" or "⬇  Download" => Get("⬇  Descargar", "⬇  Download"),
        "📋  Cola" or "📋  Queue" => Get("📋  Cola", "📋  Queue"),
        "⚙  Ajustes" or "⚙  Settings" => Get("⚙  Ajustes", "⚙  Settings"),
        "♪  Reproductor" or "♪  Player" => Get("♪  Reproductor", "♪  Player"),
        "Nueva descarga" => Get("Nueva descarga", "New download"),
        "Pegá un enlace de YouTube o SoundCloud (un elemento)" => Get("Pegá un enlace de YouTube o SoundCloud (un elemento)", "Paste a YouTube or SoundCloud link (one item)"),
        "Cola de descargas" => Get("Cola de descargas", "Download queue"),
        "Ajustes" => Get("Ajustes", "Settings"),
        "Apariencia" => Get("Apariencia", "Appearance"),
        "Tema:" => Get("Tema:", "Theme:"),
        "Idioma:" => Get("Idioma:", "Language:"),
        "Fondo" => Get("Fondo", "Canvas"),
        "Superficie" => Get("Superficie", "Surface"),
        "Campo" => Get("Campo", "Field"),
        "Acento primario" => Get("Acento primario", "Primary accent"),
        "Acento secundario" => Get("Acento secundario", "Secondary accent"),
        "Foco" => Get("Foco", "Focus"),
        "Español" => Get("Español", "Spanish"),
        "Reproductor" => Get("Reproductor", "Player"),
        "Descargar" => Get("Descargar", "Download"),
        "Pegar del portapapeles" => Get("Pegar del portapapeles", "Paste from clipboard"),
        "Enlace de descarga" => Get("Enlace de descarga", "Download link"),
        "Ayuda y resultado de validación" => Get("Ayuda y resultado de validación", "Validation help and result"),
        "Carpeta de destino" => Get("Carpeta de destino", "Destination folder"),
        "Audio:" => Get("Audio:", "Audio:"),
        "Video:" => Get("Video:", "Video:"),
        "Tipo:" => Get("Tipo:", "Type:"),
        "Formato:" => Get("Formato:", "Format:"),
        "Solo audio" => Get("Solo audio", "Audio only"),
        "Video + Audio" => Get("Video + Audio", "Video + Audio"),
        "Ajustes — Orbixa" => Get("Ajustes — Orbixa", "Orbixa Settings"),
        "⚙  ORBIXA · AJUSTES" => Get("⚙  ORBIXA · AJUSTES", "⚙  ORBIXA · SETTINGS"),
        "📁  Carpeta de destino" => Get("📁  Carpeta de destino", "📁  Destination folder"),
        "🎵  Formato por defecto" => Get("🎵  Formato por defecto", "🎵  Default format"),
        "🏷  Metadata" => Get("🏷  Metadata", "🏷  Metadata"),
        "⚡  Rendimiento" => Get("⚡  Rendimiento", "⚡  Performance"),
        "🔄  Actualizaciones" => Get("🔄  Actualizaciones", "🔄  Updates"),
        "Incrustar thumbnail en el archivo" => Get("Incrustar thumbnail en el archivo", "Embed thumbnail in file"),
        "Incrustar metadata (título, artista)" => Get("Incrustar metadata (título, artista)", "Embed metadata (title, artist)"),
        "Simultáneas (al reiniciar):" => Get("Simultáneas (al reiniciar):", "Concurrent (after restart):"),
        "Verificar actualizaciones al iniciar" => Get("Verificar actualizaciones al iniciar", "Check for updates on startup"),
        "Actualizar ahora" => Get("Actualizar ahora", "Update now"),
        "Ver detalles" => Get("Ver detalles", "View details"),
        "📂  Examinar" => Get("📂  Examinar", "📂  Browse"),
        "Audio/Video" => Get("Audio/Video", "Audio/Video"),
        "Actualizando..." => Get("Actualizando...", "Updating..."),
        "Lista de reproducción" => Get("Lista de reproducción", "Playlist"),
        "Limpiar todo" => Get("Limpiar todo", "Clear all"),
        "Sin reproducción" => Get("Sin reproducción", "Nothing playing"),
        "➕  Archivos" => Get("➕  Archivos", "➕  Files"),
        "📂  Carpeta" => Get("📂  Carpeta", "📂  Folder"),
        "No hay descargas. Pegá un enlace para empezar." => Get("No hay descargas. Pegá un enlace para empezar.", "No downloads. Paste a link to begin."),
        "Usá un enlace completo que empiece con https://" => Get("Usá un enlace completo que empiece con https://", "Enter a complete link starting with https://"),
        "Descarga agregada a la cola." => Get("Descarga agregada a la cola.", "Download added to queue."),
        "Agregando..." => Get("Agregando...", "Adding..."),
        _ => value
    };

    public static void Apply(Control root)
    {
        root.Text = Localize(root.Text);
        if (!string.IsNullOrWhiteSpace(root.AccessibleName)) root.AccessibleName = Localize(root.AccessibleName);
        foreach (Control child in root.Controls) Apply(child);
    }

    public static string LocalizeProgress(string message)
    {
        if (Language == UiLanguage.Espanol) return message;
        if (message.StartsWith("Verificando ", StringComparison.Ordinal)) return "Verifying " + message[12..];
        if (message.StartsWith("Descargando ", StringComparison.Ordinal)) return "Downloading " + message[12..];
        if (message.StartsWith("Instalación incompleta.", StringComparison.Ordinal))
        {
            int newline = message.IndexOf("\r\n", StringComparison.Ordinal);
            return newline < 0 ? "Incomplete installation. Retry from Settings -> Update now." : "Incomplete installation. Retry from Settings -> Update now." + message[newline..];
        }
        if (message == "Herramientas verificadas y actualizadas.") return "Tools verified and updated.";
        return message;
    }
    public static string StatusText(DownloadStatus status, double progress = 0) => status switch
    {
        DownloadStatus.Pending => Get("En cola", "Queued"),
        DownloadStatus.FetchingInfo => Get("Obteniendo información...", "Fetching information..."),
        DownloadStatus.Downloading => Get($"Descargando {progress:F0}%", $"Downloading {progress:F0}%"),
        DownloadStatus.Converting => Get("Convirtiendo...", "Converting..."),
        DownloadStatus.Completed => Get("Completado", "Completed"),
        DownloadStatus.Failed => Get("Error", "Error"),
        DownloadStatus.Cancelled => Get("Cancelado", "Cancelled"),
        _ => string.Empty
    };

    public static string StatusAccessible(DownloadStatus status, double progress = 0)
        => Get($"Estado: {StatusText(status, progress)}", $"Status: {StatusText(status, progress)}");
}