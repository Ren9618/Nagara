using System.Drawing;
using System.Windows.Forms;
using System.Windows.Threading;
using Nagara.App.Hooks;
using Nagara.App.Localization;
using Nagara.Core.Media;
using Nagara.Core.Routing;
using Nagara.Core.Settings;
using Application = System.Windows.Application;

namespace Nagara.App;

/// <summary>
/// Owns the notification-area icon and its context menu. Sessions are fetched
/// fresh before the menu opens so fixed-session entries reflect GSMTC now.
/// </summary>
internal sealed class TrayIconController : IDisposable
{
    private readonly AppServices _services;
    private readonly Dispatcher _dispatcher;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _icon;
    private readonly Font _headerFont;
    private ContextMenuStrip? _activeMenu;
    private bool _menuOpen;
    private bool _disposed;

    public TrayIconController(AppServices services)
    {
        _services = services;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _icon = LoadIcon();
        _headerFont = new Font(SystemFonts.MenuFont ?? SystemFonts.DefaultFont, FontStyle.Bold);

        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Visible = true,
            Text = BuildTooltip(services.Settings.Current),
        };
        _notifyIcon.MouseUp += OnMouseUp;
        _notifyIcon.DoubleClick += OnDoubleClick;

        _services.Settings.Changed += OnSettingsChanged;
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            _ = ShowMenuAsync();
        }
    }

    private void OnDoubleClick(object? sender, EventArgs e)
    {
        _services.ShowSettings();
    }

    private async Task ShowMenuAsync()
    {
        if (_menuOpen || _disposed)
        {
            return;
        }

        _menuOpen = true;
        try
        {
            var catalog = await _services.Sessions.GetCatalogAsync();
            if (_disposed)
            {
                return;
            }

            var menu = BuildMenu(catalog);
            _activeMenu = menu;
            menu.Closed += (_, _) =>
            {
                _menuOpen = false;
                if (ReferenceEquals(_activeMenu, menu))
                {
                    _activeMenu = null;
                }

                menu.Dispose();
            };

            menu.Show(Cursor.Position);
            NativeMethods.SetForegroundWindow(menu.Handle);
        }
        catch (Exception ex)
        {
            _menuOpen = false;
            _services.Log.Write("WARN", $"Could not open the tray menu: {ex.Message}");
        }
    }

    private ContextMenuStrip BuildMenu(SessionCatalog catalog)
    {
        var settings = _services.Settings.Current;
        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            ShowCheckMargin = true,
            Font = SystemFonts.MenuFont ?? SystemFonts.DefaultFont,
            BackColor = Color.FromArgb(253, 254, 254),
            ForeColor = Color.FromArgb(21, 34, 31),
            Padding = new Padding(4, 5, 4, 5),
            Renderer = new NagaraMenuRenderer(),
        };

        menu.Items.Add(new ToolStripMenuItem(Strings.Get("Menu.Destination"))
        {
            Enabled = false,
            Font = _headerFont,
            ForeColor = Color.FromArgb(94, 109, 105),
        });

        menu.Items.Add(CreateModeItem(
            Strings.Get("Menu.Spotify"),
            RoutingMode.Spotify,
            settings.Mode == RoutingMode.Spotify));

        menu.Items.Add(CreateModeItem(
            Strings.Get("Menu.Automatic"),
            RoutingMode.Automatic,
            settings.Mode == RoutingMode.Automatic));

        var fixedItem = new ToolStripMenuItem(Strings.Get("Menu.Fixed"))
        {
            Checked = settings.Mode == RoutingMode.FixedSession,
        };
        PopulateFixedSessions(fixedItem, settings, catalog);
        menu.Items.Add(fixedItem);

        menu.Items.Add(new ToolStripSeparator());

        var interceptItem = new ToolStripMenuItem(Strings.Get("Menu.Intercept"))
        {
            Checked = settings.InterceptionEnabled,
        };
        interceptItem.Click += (_, _) => ToggleInterception();
        menu.Items.Add(interceptItem);

        menu.Items.Add(new ToolStripSeparator());

        var settingsItem = new ToolStripMenuItem(Strings.Get("Menu.Settings"));
        settingsItem.Click += (_, _) => _services.ShowSettings();
        menu.Items.Add(settingsItem);

        var quitItem = new ToolStripMenuItem(Strings.Get("Menu.Quit"));
        quitItem.Click += (_, _) => _services.Exit();
        menu.Items.Add(quitItem);

        return menu;
    }

    private ToolStripMenuItem CreateModeItem(string text, RoutingMode mode, bool isChecked)
    {
        var item = new ToolStripMenuItem(text)
        {
            Checked = isChecked,
        };
        item.Click += (_, _) => SetMode(mode, null);
        return item;
    }

    private void PopulateFixedSessions(
        ToolStripMenuItem parent,
        AppSettings settings,
        SessionCatalog catalog)
    {
        var sessions = catalog.Sessions
            .GroupBy(session => session.AppUserModelId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(session => session.IsPlaying)
                .First())
            .OrderByDescending(session => session.IsPlaying)
            .ThenBy(session => session.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var savedId = settings.FixedSessionId;
        var savedIsPresent = !string.IsNullOrWhiteSpace(savedId)
            && sessions.Any(session => string.Equals(
                session.AppUserModelId,
                savedId,
                StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(savedId) && !savedIsPresent)
        {
            var missingName = settings.FixedSessionDisplayName ?? savedId;
            var missing = new ToolStripMenuItem(
                Strings.Format("Menu.MissingSession", missingName))
            {
                Checked = settings.Mode == RoutingMode.FixedSession,
                ToolTipText = savedId,
            };
            missing.Click += (_, _) => SetMode(
                RoutingMode.FixedSession,
                new MediaSessionSnapshot(savedId!, missingName, PlaybackStatus.Unknown));
            parent.DropDownItems.Add(missing);
        }

        foreach (var session in sessions)
        {
            var item = new ToolStripMenuItem(session.DisplayName)
            {
                Checked = settings.Mode == RoutingMode.FixedSession
                    && string.Equals(
                        savedId,
                        session.AppUserModelId,
                        StringComparison.OrdinalIgnoreCase),
                ToolTipText = session.AppUserModelId,
            };
            item.Click += (_, _) => SetMode(RoutingMode.FixedSession, session);
            parent.DropDownItems.Add(item);
        }

        if (parent.DropDownItems.Count == 0)
        {
            parent.DropDownItems.Add(new ToolStripMenuItem(Strings.Get("Menu.NoSessions"))
            {
                Enabled = false,
            });
        }
    }

    private void SetMode(RoutingMode mode, MediaSessionSnapshot? fixedSession)
    {
        try
        {
            _services.Settings.Update(current =>
            {
                var next = current with { Mode = mode };
                if (fixedSession is not null)
                {
                    next = next with
                    {
                        FixedSessionId = fixedSession.AppUserModelId,
                        FixedSessionDisplayName = fixedSession.DisplayName,
                    };
                }

                return next;
            });
        }
        catch (Exception ex)
        {
            ShowBalloon(
                Strings.Format("Tray.SaveFailed", ex.Message),
                ToolTipIcon.Warning);
            _services.Log.Write("WARN", $"Could not save the tray selection: {ex.Message}");
            return;
        }

        UpdateTooltip();
    }

    private void ToggleInterception()
    {
        var enabled = !_services.Settings.Current.InterceptionEnabled;

        try
        {
            _services.Settings.Update(current => current with
            {
                InterceptionEnabled = enabled,
            });
        }
        catch (Exception ex)
        {
            ShowBalloon(
                Strings.Format("Tray.SaveFailed", ex.Message),
                ToolTipIcon.Warning);
            _services.Log.Write("WARN", $"Could not save the capture setting: {ex.Message}");
            return;
        }

        var error = _services.SetInterception(enabled);
        if (error is not null)
        {
            ShowBalloon(
                Strings.Format("Settings.HookFailed", error),
                ToolTipIcon.Warning);
        }

        UpdateTooltip();
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        if (_dispatcher.CheckAccess())
        {
            UpdateTooltip();
        }
        else
        {
            _dispatcher.BeginInvoke(new Action(UpdateTooltip));
        }
    }

    private void UpdateTooltip()
    {
        _notifyIcon.Text = BuildTooltip(_services.Settings.Current);
    }

    private static string BuildTooltip(AppSettings settings)
    {
        var mode = UiText.Mode(settings.Mode);
        var text = settings.InterceptionEnabled
            ? Strings.Format("Tray.Tooltip", mode)
            : Strings.Format("Tray.TooltipOff", mode);

        return text.Length > 63 ? text[..63] : text;
    }

    private void ShowBalloon(string text, ToolTipIcon icon)
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = Strings.Get("App.Tagline");
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.ShowBalloonTip(5000);
    }

    private static Icon LoadIcon()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(path))
            {
                var extracted = Icon.ExtractAssociatedIcon(path);
                if (extracted is not null)
                {
                    return extracted;
                }
            }
        }
        catch (Exception)
        {
            // Fall through to the generic icon.
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _services.Settings.Changed -= OnSettingsChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.MouseUp -= OnMouseUp;
        _notifyIcon.DoubleClick -= OnDoubleClick;
        _notifyIcon.Dispose();

        _activeMenu?.Dispose();
        _activeMenu = null;
        _headerFont.Dispose();
        _icon.Dispose();
    }
}
