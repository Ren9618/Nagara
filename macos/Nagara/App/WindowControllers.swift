import AppKit
import SwiftUI

@MainActor
final class SettingsWindowController: NSWindowController {
    init(
        settings: SettingsStore,
        loginItem: LoginItemManager,
        permissions: PermissionManager,
        onRecheckAccessibility: @escaping () -> Void,
        onCheckMusicAutomation: @escaping () -> Void
    ) {
        let hostingController = NSHostingController(
            rootView: SettingsView(
                settings: settings,
                loginItem: loginItem,
                permissions: permissions,
                onRecheckAccessibility: onRecheckAccessibility,
                onCheckMusicAutomation: onCheckMusicAutomation
            )
        )
        let window = NSWindow(contentViewController: hostingController)
        window.title = L10n.text("settings.title")
        window.styleMask = [.titled, .closable, .miniaturizable]
        window.isReleasedWhenClosed = false
        window.center()

        super.init(window: window)
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    override func showWindow(_ sender: Any?) {
        super.showWindow(sender)
        window?.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }
}

@MainActor
final class PermissionGuidanceWindowController: NSWindowController {
    init(
        permissions: PermissionManager,
        onRecheckAccessibility: @escaping () -> Void,
        onCheckMusicAutomation: @escaping () -> Void
    ) {
        let window = NSWindow()
        window.title = L10n.text("permission.title")
        window.styleMask = [.titled, .closable]
        window.isReleasedWhenClosed = false
        window.center()

        super.init(window: window)

        let view = PermissionGuidanceView(
            permissions: permissions,
            onRecheckAccessibility: onRecheckAccessibility,
            onCheckMusicAutomation: onCheckMusicAutomation,
            onClose: { [weak self] in self?.close() }
        )
        window.contentViewController = NSHostingController(rootView: view)
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    override func showWindow(_ sender: Any?) {
        super.showWindow(sender)
        window?.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }
}
