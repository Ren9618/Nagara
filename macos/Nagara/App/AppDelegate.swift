import AppKit

@main
@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate {
    private let settings = SettingsStore()
    private let permissions = PermissionManager()
    private let loginItem = LoginItemManager()
    private let playerController = AppleScriptPlayerController()
    private let mediaKeyTap = MediaKeyTap()

    private lazy var router = MediaKeyRouter(
        controller: playerController,
        mode: settings.mode,
        lastPlaying: settings.lastPlaying
    )

    private var statusMenuController: StatusMenuController?
    private var settingsWindowController: SettingsWindowController?
    private var permissionWindowController: PermissionGuidanceWindowController?

    static func main() {
        let application = NSApplication.shared
        let delegate = AppDelegate()
        application.delegate = delegate
        application.setActivationPolicy(.accessory)
        application.run()
    }

    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.accessory)

        configurePlayerController()
        configureRouter()
        configureMediaKeyTap()
        configureMenu()
        observeSettings()
        applyInterceptionState()

        if !permissions.accessibilityGranted {
            showPermissionGuidance()
        }
    }

    func applicationDidBecomeActive(_ notification: Notification) {
        let granted = permissions.refreshAccessibility()
        loginItem.refresh()

        if granted && settings.interceptionEnabled {
            if mediaKeyTap.start() {
                permissions.setEventTapActive(true)
            }
        } else if !granted {
            mediaKeyTap.stop()
            permissions.setEventTapActive(false)
        }

        statusMenuController?.refresh()
    }

    func applicationWillTerminate(_ notification: Notification) {
        mediaKeyTap.stop()
    }

    private func configurePlayerController() {
        playerController.onAutomationDenied = { [weak self] target in
            guard let self else {
                return
            }
            let isFirstDenial = self.permissions.noteAutomationDenied(target)
            self.statusMenuController?.refresh()
            if isFirstDenial {
                self.showPermissionGuidance()
            }
        }

        playerController.onAutomationSucceeded = { [weak self] target in
            self?.permissions.noteAutomationSucceeded(target)
            self?.statusMenuController?.refresh()
        }
    }

    private func configureRouter() {
        router.updateLastPlaying(settings.lastPlaying)
        router.onLastPlayingChange = { [weak self] target in
            self?.settings.setLastPlaying(target)
        }
    }

    private func configureMediaKeyTap() {
        mediaKeyTap.onAction = { [weak self] action in
            DispatchQueue.main.async {
                self?.router.handle(action)
            }
        }

        mediaKeyTap.onTapFailure = { [weak self] in
            self?.permissions.setEventTapActive(false)
            self?.showPermissionGuidance()
        }
    }

    private func configureMenu() {
        statusMenuController = StatusMenuController(
            settings: settings,
            permissions: permissions,
            onOpenSettings: { [weak self] in
                self?.showSettings()
            },
            onOpenPermissionGuidance: { [weak self] in
                self?.showPermissionGuidance()
            },
            onQuit: {
                NSApp.terminate(nil)
            }
        )

        statusMenuController?.onMenuWillOpen = { [weak self] in
            self?.refreshPermissionState()
        }
    }

    /// Re-checks Accessibility without showing the guidance window. This lets
    /// the user grant permission in System Settings and then simply open the
    /// status menu to activate interception.
    private func refreshPermissionState() {
        let granted = permissions.refreshAccessibility()

        if granted {
            if settings.interceptionEnabled && !mediaKeyTap.isRunning {
                if mediaKeyTap.start() {
                    permissions.setEventTapActive(true)
                }
            }
        } else {
            mediaKeyTap.stop()
            permissions.setEventTapActive(false)
        }

        statusMenuController?.refresh()
    }

    private func observeSettings() {
        settings.onModeChange = { [weak self] mode in
            self?.router.updateMode(mode)
            self?.statusMenuController?.refresh()
        }

        settings.onInterceptionChange = { [weak self] _ in
            self?.applyInterceptionState()
        }
    }

    private func applyInterceptionState() {
        permissions.refreshAccessibility()
        mediaKeyTap.isEnabled = settings.interceptionEnabled

        guard settings.interceptionEnabled, permissions.accessibilityGranted else {
            mediaKeyTap.stop()
            permissions.setEventTapActive(false)
            statusMenuController?.refresh()
            return
        }

        if mediaKeyTap.start() {
            permissions.setEventTapActive(true)
        } else {
            permissions.setEventTapActive(false)
            showPermissionGuidance()
        }
        statusMenuController?.refresh()
    }

    private func showSettings() {
        permissions.refreshAccessibility()
        loginItem.refresh()

        if settingsWindowController == nil {
            settingsWindowController = SettingsWindowController(
                settings: settings,
                loginItem: loginItem,
                permissions: permissions,
                onRecheckAccessibility: { [weak self] in
                    self?.recheckAccessibility()
                },
                onCheckMusicAutomation: { [weak self] in
                    self?.checkMusicAutomation()
                }
            )
        }
        settingsWindowController?.showWindow(nil)
    }

    private func showPermissionGuidance() {
        permissions.refreshAccessibility()

        if permissionWindowController == nil {
            permissionWindowController = PermissionGuidanceWindowController(
                permissions: permissions,
                onRecheckAccessibility: { [weak self] in
                    self?.recheckAccessibility()
                },
                onCheckMusicAutomation: { [weak self] in
                    self?.checkMusicAutomation()
                }
            )
        }
        permissionWindowController?.showWindow(nil)
    }

    /// Re-reads the Accessibility state and immediately applies it, so the
    /// user does not have to switch apps or restart after granting permission.
    private func recheckAccessibility() {
        permissions.refreshAccessibility()
        applyInterceptionState()
        statusMenuController?.refresh()
    }

    /// Sends a harmless `player state` query to Apple Music. This is the first
    /// Apple Event, so macOS shows the Automation prompt if it has not been
    /// answered yet. It never starts playback.
    private func checkMusicAutomation() {
        permissions.refreshAccessibility()
        playerController.probeAutomation(for: .appleMusic)
        statusMenuController?.refresh()
    }
}
