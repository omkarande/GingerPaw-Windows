using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace GingerPaw.App;

public partial class App : Application
{
    // TEMPORARY DIAGNOSTIC — delete this whole logging mechanism (LogPath, Log(), and every
    // call site in AppRuntime.cs) once the full record->transcribe->paste loop is confirmed
    // working reliably end-to-end. Mirrors every line to both the console (visible when
    // launched via `dotnet run`/F5 from an existing terminal) and a file (visible even when
    // double-clicked from Explorer, which has no console to inherit).
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "gingerpaw-startup.log");

    private AppRuntime? _runtime;

    internal static void Log(string message)
    {
        var line = $"{DateTime.Now:O} {message}";
        Console.WriteLine(line);
        File.AppendAllText(LogPath, line + "\n");
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            Log("OnStartup begin");
            var services = await AppComposition.MakeAsync();
            Log("AppComposition.MakeAsync completed");
            _runtime = new AppRuntime(services);
            _runtime.Start();
            Log($"AppRuntime started, tray IsCreated={services.Tray.IsCreated}");
        }
        catch (Exception ex)
        {
            Log($"STARTUP FAILED: {ex}");
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log($"UNHANDLED: {e.Exception}");
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _runtime?.Stop();
        base.OnExit(e);
    }
}
