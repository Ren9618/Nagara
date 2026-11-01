using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Threading;
using Nagara.App.Diagnostics;
using Nagara.App.Hooks;
using Nagara.App.Localization;
using Nagara.App.UI;
using Nagara.Core.Localization;
using Nagara.Core.Media;
using Nagara.Core.Routing;
using Nagara.Core.Settings;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Nagara.App;

/// <summary>
/// Composition root for the tray application. It owns the long-lived services,
/// the single-instance guard, and the serialized media-key command worker.
/// </summary>
public partial class App : Application
{
    private const string MutexName = @"Local\Nagara.Windows.v0.1";
    private const string ShowSettingsEventName = @"Local\Nagara.Windows.v0.1.ShowSettings";

    private readonly CancellationTokenSource _shutdown = new();

    private SingleInstanceCoordinator? _singleInstance;
    private SettingsStore? _settings;
    private DiagnosticsLog? _log;
    private DiagnosticsState? _diagnostics;
    private GsmtcSessionService? _sessions;
    private KeyboardHookService? _hook;
    private TrayIconController? _tray;
    private RouteCommandHandler? _routeHandler;
    private Channel<MediaKeyCommand>? _commands;
    private Task? _commandLoop;
    private SettingsWindow? _settingsWindow;
    private AppServices? _services;
    private bool _isShuttingDown;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            Start();
        }
        catch (Exception ex)
        {
            _log?.Write("ERROR", $"Startup failed: {ex}");
            MessageBox.Show(
                Strings.Format("App.UnexpectedError", ex.Message),
                "Nagara",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void Start()
    {
        _singleInstance = SingleInstanceCoordinator.Create(
            MutexName,
            ShowSettingsEventName,
            ShowSettings);

        if (!_singleInstance.IsPrimary)
        {
            _singleInstance.SignalPrimary();
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        _settings = new SettingsStore(AppPaths.SettingsFile);
        ApplyLanguage(_settings.Current.Language);

        _log = new DiagnosticsLog(AppPaths.LogFile);
        _diagnostics = new DiagnosticsState();
        _sessions = new GsmtcSessionService(_log);

        if (!string.IsNullOrWhiteSpace(_settings.LastLoadError))
        {
            _log.Write("WARN", $"The settings file could not be read: {_settings.LastLoadError}");
            _diagnostics.RecordError(
                Strings.Format("Diagnostics.SettingsLoadFailed", _settings.LastLoadError));
        }

        _routeHandler = new RouteCommandHandler(
            _settings,
            _sessions,
            new RouteResolver(),
            _diagnostics,
            _log,
            new SpotifyLauncher(_log));

        _commands = Channel.CreateBounded<MediaKeyCommand>(
            new BoundedChannelOptions(32)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest,
            });

        _hook = new KeyboardHookService(
            new MediaKeyProcessor(),
            command => _commands?.Writer.TryWrite(command));

        var startup = new StartupRegistration(AppPaths.ExecutablePath);

        _services = new AppServices
        {
            Settings = _settings,
            Sessions = _sessions,
            Diagnostics = _diagnostics,
            Log = _log,
            Startup = startup,
            Version = AppPaths.Version,
            ExecutablePath = AppPaths.ExecutablePath,
            SettingsPath = AppPaths.SettingsFile,
            LogFolderPath = AppPaths.RootDirectory,
            SetInterception = SetInterception,
            ShowSettings = ShowSettings,
            Exit = ExitApplication,
            OpenLogFolder = OpenLogFolder,
        };

        _tray = new TrayIconController(_services);

        var startupError = startup.Sync(_settings.Current.LaunchAtLogin);
        if (startupError is not null)
        {
            _log.Write("WARN", $"Could not synchronize launch at sign-in: {startupError}");
            _diagnostics.RecordError(
                Strings.Format("Diagnostics.StartupSyncFailed", startupError));
        }

        if (_settings.Current.InterceptionEnabled)
        {
            SetInterception(true);
        }
        else
        {
            _diagnostics.SetHookState(HookState.Inactive);
        }

        _commandLoop = Task.Run(() => ProcessCommandsAsync(
            _commands.Reader,
            _routeHandler,
            _shutdown.Token));

        _log.Write(
            "INFO",
            $"Nagara {AppPaths.Version} started (capture={_settings.Current.InterceptionEnabled}, mode={_settings.Current.Mode}).");
    }

    private async Task ProcessCommandsAsync(
        ChannelReader<MediaKeyCommand> reader,
        RouteCommandHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var command in reader
                .ReadAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                try
                {
                    await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _log?.Write("ERROR", $"Media key worker failed: {ex}");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private string? SetInterception(bool enabled)
    {
        if (_hook is null)
        {
            return Strings.Get("Diagnostics.HookNotInitialized");
        }

        try
        {
            if (enabled)
            {
                _hook.Start();
                _diagnostics?.SetHookState(HookState.Active);
                _log?.Write("INFO", "Media key capture enabled.");
            }
            else
            {
                _hook.Stop();
                _diagnostics?.SetHookState(HookState.Inactive);
                _log?.Write("INFO", "Media key capture disabled.");
            }

            return null;
        }
        catch (Exception ex)
        {
            _diagnostics?.SetHookState(HookState.Failed, ex.Message);
            _log?.Write("ERROR", $"Could not change media key capture: {ex.Message}");
            return ex.Message;
        }
    }

    private void ShowSettings()
    {
        if (_isShuttingDown)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(ShowSettings));
            }
            catch (InvalidOperationException)
            {
                // The dispatcher is shutting down.
            }

            return;
        }

        if (_services is null)
        {
            return;
        }

        if (_settingsWindow is not null)
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
            {
                _settingsWindow.WindowState = WindowState.Normal;
            }

            _settingsWindow.Activate();
            _settingsWindow.Focus();
            return;
        }

        var window = new SettingsWindow(_services);
        _settingsWindow = window;
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_settingsWindow, window))
            {
                _settingsWindow = null;
            }
        };
        window.Show();
        window.Activate();
    }

    private void ExitApplication()
    {
        if (!Dispatcher.CheckAccess())
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(ExitApplication));
            }
            catch (InvalidOperationException)
            {
                // The dispatcher is shutting down.
            }

            return;
        }

        Shutdown();
    }

    private string? OpenLogFolder()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.RootDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = AppPaths.RootDirectory,
                UseShellExecute = true,
            });
            return null;
        }
        catch (Exception ex)
        {
            _log?.Write("WARN", $"Could not open the diagnostics folder: {ex.Message}");
            return ex.Message;
        }
    }

    private static void ApplyLanguage(AppLanguage language)
    {
        var uiLanguage = LanguageResolver.Resolve(language, CultureInfo.CurrentUICulture);
        var culture = CultureInfo.GetCultureInfo(
            uiLanguage == UiLanguage.Japanese ? "ja-JP" : "en-US");
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    private void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        _log?.Write("ERROR", $"Unhandled UI exception: {e.Exception}");
        if (!_isShuttingDown)
        {
            MessageBox.Show(
                Strings.Format("App.UnexpectedError", e.Exception.Message),
                "Nagara",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        => _log?.Write("ERROR", $"Unhandled exception: {e.ExceptionObject}");

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _log?.Write("ERROR", $"Unobserved task exception: {e.Exception}");
        e.SetObserved();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isShuttingDown = true;

        try
        {
            _hook?.Dispose();
        }
        catch (Exception)
        {
            // Shutdown must continue even if a native cleanup call fails.
        }

        try
        {
            _tray?.Dispose();
        }
        catch (Exception)
        {
            // Shutdown must continue even if the tray icon is already gone.
        }

        try
        {
            _settingsWindow?.Close();
        }
        catch (Exception)
        {
            // The window may already be closing.
        }

        _shutdown.Cancel();
        try
        {
            _commandLoop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception)
        {
            // A stuck worker must not prevent the process from exiting.
        }

        _sessions?.Dispose();
        _singleInstance?.Dispose();
        _shutdown.Dispose();
        _log?.Write("INFO", "Nagara stopped.");

        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        base.OnExit(e);
    }
}
