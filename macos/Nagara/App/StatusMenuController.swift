import AppKit

@MainActor
final class StatusMenuController: NSObject, NSMenuDelegate {
    private let statusItem: NSStatusItem
    private let settings: SettingsStore
    private let permissions: PermissionManager
    private let onOpenSettings: () -> Void
    private let onOpenPermissionGuidance: () -> Void
    private let onQuit: () -> Void

    /// Called before the menu is rebuilt so the app can refresh permission
    /// state (for example right after the user grants Accessibility).
    var onMenuWillOpen: (() -> Void)?

    init(
        settings: SettingsStore,
        permissions: PermissionManager,
        onOpenSettings: @escaping () -> Void,
        onOpenPermissionGuidance: @escaping () -> Void,
        onQuit: @escaping () -> Void
    ) {
        self.statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
        self.settings = settings
        self.permissions = permissions
        self.onOpenSettings = onOpenSettings
        self.onOpenPermissionGuidance = onOpenPermissionGuidance
        self.onQuit = onQuit

        super.init()

        let menu = NSMenu()
        menu.delegate = self
        statusItem.menu = menu
        refresh()
    }

    func refresh() {
        guard let button = statusItem.button else {
            return
        }

        let symbolName = settings.interceptionEnabled ? "play.circle" : "pause.circle"
        let image = NSImage(
            systemSymbolName: symbolName,
            accessibilityDescription: L10n.text("statusItem.accessibility")
        )
        image?.isTemplate = true
        button.image = image
        button.toolTip = L10n.format("statusItem.tooltip", L10n.modeName(settings.mode))
    }

    func menuNeedsUpdate(_ menu: NSMenu) {
        onMenuWillOpen?()
        menu.removeAllItems()

        if !permissions.accessibilityGranted {
            addWarningItem(
                titleKey: "menu.permission.accessibility",
                action: #selector(openPermissionGuidance),
                to: menu
            )
        }

        if !permissions.automationDeniedTargets.isEmpty {
            addWarningItem(
                titleKey: "menu.permission.automation",
                action: #selector(openPermissionGuidance),
                to: menu
            )
        }

        if !menu.items.isEmpty {
            menu.addItem(.separator())
        }

        for mode in RoutingMode.allCases {
            let item = NSMenuItem(
                title: L10n.modeName(mode),
                action: #selector(selectMode(_:)),
                keyEquivalent: ""
            )
            item.target = self
            item.representedObject = mode.rawValue
            item.state = settings.mode == mode ? .on : .off
            menu.addItem(item)
        }

        menu.addItem(.separator())

        let captureItem = NSMenuItem(
            title: L10n.text("menu.capture"),
            action: #selector(toggleInterception),
            keyEquivalent: ""
        )
        captureItem.target = self
        captureItem.state = settings.interceptionEnabled ? .on : .off
        menu.addItem(captureItem)

        menu.addItem(.separator())

        let settingsItem = NSMenuItem(
            title: L10n.text("menu.settings"),
            action: #selector(openSettings),
            keyEquivalent: ","
        )
        settingsItem.target = self
        settingsItem.keyEquivalentModifierMask = [.command]
        menu.addItem(settingsItem)

        menu.addItem(.separator())

        let quitItem = NSMenuItem(
            title: L10n.text("menu.quit"),
            action: #selector(quit),
            keyEquivalent: "q"
        )
        quitItem.target = self
        quitItem.keyEquivalentModifierMask = [.command]
        menu.addItem(quitItem)
    }

    private func addWarningItem(titleKey: String, action: Selector, to menu: NSMenu) {
        let item = NSMenuItem(title: L10n.text(titleKey), action: action, keyEquivalent: "")
        item.target = self
        menu.addItem(item)
    }

    @objc private func selectMode(_ sender: NSMenuItem) {
        guard
            let rawValue = sender.representedObject as? String,
            let mode = RoutingMode(rawValue: rawValue)
        else {
            return
        }
        settings.mode = mode
        refresh()
    }

    @objc private func toggleInterception() {
        settings.interceptionEnabled.toggle()
        refresh()
    }

    @objc private func openSettings() {
        onOpenSettings()
    }

    @objc private func openPermissionGuidance() {
        onOpenPermissionGuidance()
    }

    @objc private func quit() {
        onQuit()
    }
}
