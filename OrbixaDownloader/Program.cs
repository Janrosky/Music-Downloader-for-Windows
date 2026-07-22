using OrbixaDownloader.Forms;
using OrbixaDownloader.Models;
using OrbixaDownloader.Services;

namespace OrbixaDownloader
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.ThreadException += (_, e) =>
                MessageBox.Show($"Error inesperado:\n{e.Exception.Message}",
                    "Orbixa Downloader", MessageBoxButtons.OK, MessageBoxIcon.Error);

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                MessageBox.Show($"Error crítico:\n{e.ExceptionObject}",
                    "Orbixa Downloader", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // Cargar settings
            var settings = AppSettings.Load();

            // Mostrar splash, luego pasar al MainForm como ventana principal
            using var splash = new SplashForm(settings);
            splash.ShowDialog(); // bloqueante — espera a que el splash cierre

            // Splash cerrado — ahora arrancar MainForm como loop principal
            Application.Run(new MainForm(settings));
        }
    }
}