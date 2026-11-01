using System.Windows;
using Nagara.App.Diagnostics;
using Nagara.App.Localization;
using Nagara.Core.Media;
using Nagara.Core.Routing;
using Nagara.Core.Settings;
using MediaBrush = System.Windows.Media.Brush;
using WpfClipboard = System.Windows.Clipboard;

namespace Nagara.App.UI;

public partial class SettingsWindow : Window
{
    private readonly AppServices _services;
    private readonly List<LanguageChoice> _languageChoices;
    private readonly EventHandler _diagnosticsChangedHandler;

    private CancellationTokenSource? _sessionLoadCancellation;
    private SessionCatalog? _catalog;
    private string? _persistentWarning;
    private bool _closed;
    private bool _loadingSessions;

    internal SettingsWindow(AppServices services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = services;
        InitializeComponent();

        _diagnosticsChangedHandler = OnDiagnosticsChanged;
        _languageChoices =
        [
            new LanguageChoice(AppLanguage.Auto, Strings.Get("Settings.LanguageAuto")),
            new LanguageChoice(AppLanguage.Japanese, Strings.Get("Settings.LanguageJapanese")),
            new LanguageChoice(AppLanguage.English, Strings.Get("Settings.LanguageEnglish")),
        ];

        LanguageCombo.ItemsSource = _languageChoices;
        VersionValue.Text = services.Version;
        ExecutablePathValue.Text = services.ExecutablePath;
        SettingsPathValue.Text = services.SettingsPath;

        if (!string.IsNullOrWhiteSpace(services.Settings.LastLoadError))
        {
            _persistentWarning = Strings.Get("Settings.LoadFailed");
        }

        LoadSettings();
        UpdateDiagnostics();
        SetStatus(_persistentWarning ?? string.Empty, _persistentWarning is not null);

        _services.Diagnostics.Changed += _diagnosticsChangedHandler;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void LoadSettings()
    {
        var settings = _services.Settings.Current;

        SpotifyRadio.IsChecked = settings.Mode == RoutingMode.Spotify;
        AutomaticRadio.IsChecked = settings.Mode == RoutingMode.Automatic;
        FixedRadio.IsChecked = settings.Mode == RoutingMode.FixedSession;

        InterceptCheck.IsChecked = settings.InterceptionEnabled;
        LaunchAtLoginCheck.IsChecked = settings.LaunchAtLogin;

        LanguageCombo.SelectedItem = _languageChoices
            .FirstOrDefault(choice => choice.Value == settings.Language)
            ?? _languageChoices[0];

        UpdateModeControls();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await RefreshSessionsAsync();
    }

    private void OnModeChanged(object sender, RoutedEventArgs e) => UpdateModeControls();

    private void UpdateModeControls()
    {
        var fixedMode = FixedRadio.IsChecked == true;
        SessionCombo.IsEnabled = fixedMode && !_loadingSessions;
        RefreshButton.IsEnabled = fixedMode && !_loadingSessions;

        NoSessionsText.Visibility = fixedMode
            && !_loadingSessions
            && SessionCombo.Items.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private async Task RefreshSessionsAsync()
    {
        if (_closed || _loadingSessions)
        {
            return;
        }

        _loadingSessions = true;
        UpdateModeControls();
        SetStatus(Strings.Get("Settings.Loading"), isError: false);
        DiagnosticsErrorText.Visibility = Visibility.Collapsed;

        _sessionLoadCancellation?.Cancel();
        _sessionLoadCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _sessionLoadCancellation = cancellation;

        try
        {
            var catalog = await _services.Sessions
                .GetCatalogAsync(cancellation.Token)
                .ConfigureAwait(true);

            if (_closed || cancellation.IsCancellationRequested)
            {
                return;
            }

            _catalog = catalog;
            PopulateSessions(catalog);
            PopulateDiagnosticsSessions(catalog);
            UpdateDiagnostics();
            SetStatus(_persistentWarning ?? string.Empty, _persistentWarning is not null);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // The window is closing.
        }
        catch (Exception ex)
        {
            if (_closed || cancellation.IsCancellationRequested)
            {
                return;
            }

            var message = Strings.Format("Settings.RefreshFailed", ex.Message);
            DiagnosticsErrorText.Text = message;
            DiagnosticsErrorText.Visibility = Visibility.Visible;
            SetStatus(message, isError: true);
            _services.Log.Write("WARN", $"Could not refresh the session list: {ex.Message}");
        }
        finally
        {
            if (ReferenceEquals(_sessionLoadCancellation, cancellation))
            {
                _sessionLoadCancellation = null;
            }

            cancellation.Dispose();
            if (!_closed)
            {
                _loadingSessions = false;
                UpdateModeControls();
            }
        }
    }

    private void PopulateSessions(SessionCatalog catalog)
    {
        var settings = _services.Settings.Current;
        var previousId = (SessionCombo.SelectedItem as SessionChoice)?.AppUserModelId;
        var savedId = settings.FixedSessionId;

        var sessions = catalog.Sessions
            .GroupBy(session => session.AppUserModelId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(session => session.IsPlaying)
                .First())
            .OrderByDescending(session => session.IsPlaying)
            .ThenBy(session => session.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var choices = sessions
            .Select(session => new SessionChoice(
                session,
                Strings.Format(
                    "Settings.SessionLabel",
                    session.DisplayName,
                    UiText.SessionStatus(session.Status))))
            .ToList();

        var savedIsPresent = !string.IsNullOrWhiteSpace(savedId)
            && choices.Any(choice => string.Equals(
                choice.AppUserModelId,
                savedId,
                StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(savedId) && !savedIsPresent)
        {
            var missingName = settings.FixedSessionDisplayName ?? savedId;
            choices.Insert(0, new SessionChoice(
                savedId,
                missingName,
                Strings.Format("Settings.SessionMissing", missingName)));
        }

        var preferredId = string.IsNullOrWhiteSpace(previousId) ? savedId : previousId;
        var selected = choices.FirstOrDefault(choice => string.Equals(
                choice.AppUserModelId,
                preferredId,
                StringComparison.OrdinalIgnoreCase))
            ?? choices.FirstOrDefault();

        SessionCombo.ItemsSource = choices;
        SessionCombo.SelectedItem = selected;
        UpdateModeControls();
    }

    private void PopulateDiagnosticsSessions(SessionCatalog catalog)
    {
        var rows = catalog.Sessions
            .OrderByDescending(session => string.Equals(
                session.AppUserModelId,
                catalog.CurrentSessionId,
                StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(session => session.IsPlaying)
            .ThenBy(session => session.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(session =>
            {
                var isCurrent = string.Equals(
                    session.AppUserModelId,
                    catalog.CurrentSessionId,
                    StringComparison.OrdinalIgnoreCase);
                var status = UiText.SessionStatus(session.Status);
                if (isCurrent)
                {
                    status = $"{status} · {Strings.Get("Common.Current")}";
                }

                return new SessionRow
                {
                    DisplayName = session.DisplayName,
                    AppUserModelId = session.AppUserModelId,
                    StatusText = status,
                };
            })
            .ToList();

        SessionsList.ItemsSource = rows;
        SessionsList.Visibility = rows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        DiagnosticsSessionsEmpty.Visibility = rows.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnRefreshSessions(object sender, RoutedEventArgs e)
        => _ = RefreshSessionsAsync();

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var current = _services.Settings.Current;
        var mode = FixedRadio.IsChecked == true
            ? RoutingMode.FixedSession
            : AutomaticRadio.IsChecked == true
                ? RoutingMode.Automatic
                : RoutingMode.Spotify;

        var selection = SessionCombo.SelectedItem as SessionChoice;
        if (mode == RoutingMode.FixedSession && selection is null)
        {
            SetStatus(Strings.Get("Settings.SelectSessionRequired"), isError: true);
            SessionCombo.Focus();
            return;
        }

        var language = (LanguageCombo.SelectedItem as LanguageChoice)?.Value
            ?? current.Language;

        var updated = current with
        {
            Mode = mode,
            InterceptionEnabled = InterceptCheck.IsChecked == true,
            LaunchAtLogin = LaunchAtLoginCheck.IsChecked == true,
            Language = language,
        };

        if (mode == RoutingMode.FixedSession && selection is not null)
        {
            updated = updated with
            {
                FixedSessionId = selection.AppUserModelId,
                FixedSessionDisplayName = selection.DisplayName,
            };
        }

        try
        {
            _services.Settings.Save(updated);
            _persistentWarning = null;
        }
        catch (Exception ex)
        {
            SetStatus(Strings.Format("Settings.SaveFailed", ex.Message), isError: true);
            _services.Log.Write("WARN", $"Could not save settings: {ex.Message}");
            return;
        }

        string? startupError = null;
        try
        {
            startupError = _services.Startup.Sync(updated.LaunchAtLogin);
        }
        catch (Exception ex)
        {
            startupError = ex.Message;
        }

        if (startupError is not null)
        {
            _services.Log.Write("WARN", $"Could not update startup registration: {startupError}");
            _services.Diagnostics.RecordError(
                Strings.Format("Diagnostics.StartupUpdateFailed", startupError));
        }

        string? hookError;
        try
        {
            hookError = _services.SetInterception(updated.InterceptionEnabled);
        }
        catch (Exception ex)
        {
            hookError = ex.Message;
        }

        UpdateDiagnostics();

        if (hookError is not null)
        {
            SetStatus(Strings.Format("Settings.HookFailed", hookError), isError: true);
        }
        else if (startupError is not null)
        {
            SetStatus(Strings.Format("Settings.StartupFailed", startupError), isError: true);
        }
        else
        {
            SetStatus(Strings.Get("Settings.Saved"), isError: false);
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private void OnCopyDiagnostics(object sender, RoutedEventArgs e)
    {
        try
        {
            var report = DiagnosticsReportBuilder.Build(_services, _catalog);
            WpfClipboard.SetText(report);
            SetStatus(Strings.Get("Settings.Copied"), isError: false);
        }
        catch (Exception ex)
        {
            var message = Strings.Format("Settings.CopyFailed", ex.Message);
            SetStatus(message, isError: true);
            _services.Log.Write("WARN", $"Could not copy diagnostics: {ex.Message}");
        }
    }

    private void OnOpenLogFolder(object sender, RoutedEventArgs e)
    {
        try
        {
            var error = _services.OpenLogFolder();
            if (error is null)
            {
                SetStatus(Strings.Get("Settings.LogFolderOpened"), isError: false);
            }
            else
            {
                SetStatus(
                    Strings.Format("Settings.OpenLogFolderFailed", error),
                    isError: true);
            }
        }
        catch (Exception ex)
        {
            SetStatus(
                Strings.Format("Settings.OpenLogFolderFailed", ex.Message),
                isError: true);
            _services.Log.Write("WARN", $"Could not open the log folder: {ex.Message}");
        }
    }

    private void OnDiagnosticsChanged(object? sender, EventArgs e)
    {
        if (_closed)
        {
            return;
        }

        if (Dispatcher.CheckAccess())
        {
            UpdateDiagnostics();
        }
        else
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(UpdateDiagnostics));
            }
            catch (InvalidOperationException)
            {
                // The dispatcher is shutting down.
            }
        }
    }

    private void UpdateDiagnostics()
    {
        if (_closed)
        {
            return;
        }

        var snapshot = _services.Diagnostics.Snapshot();
        HookStatusValue.Text = UiText.Hook(snapshot);
        LastKeyValue.Text = UiText.CommandWithTime(snapshot);
        LastTargetValue.Text = UiText.LastTarget(snapshot);

        LastErrorValue.Text = string.IsNullOrWhiteSpace(snapshot.LastError)
            ? Strings.Get("Diagnostics.None")
            : snapshot.LastErrorAt is { } at
                ? $"{snapshot.LastError} ({at:HH:mm:ss})"
                : snapshot.LastError;
    }

    private void SetStatus(string text, bool isError)
    {
        StatusText.Text = text;
        StatusText.Foreground = (MediaBrush)FindResource(
            isError ? "DangerBrush" : "SecondaryTextBrush");
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        _sessionLoadCancellation?.Cancel();
        _services.Diagnostics.Changed -= _diagnosticsChangedHandler;
        Closed -= OnClosed;
    }
}
