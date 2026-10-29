import AppKit
import SwiftUI

struct SettingsView: View {
    @ObservedObject var settings: SettingsStore
    @ObservedObject var loginItem: LoginItemManager
    @ObservedObject var permissions: PermissionManager

    let onRecheckAccessibility: () -> Void
    let onCheckMusicAutomation: () -> Void

    @State private var diagnosticsExpanded = false

    private var version: String {
        Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "0.1.0"
    }

    private var executionPath: String {
        Bundle.main.bundlePath
    }

    private var abbreviatedExecutionPath: String {
        let path = executionPath
        guard path.count > 52 else {
            return path
        }
        return "\(path.prefix(26))…\(path.suffix(22))"
    }

    private func copyExecutionPath() {
        let pasteboard = NSPasteboard.general
        pasteboard.clearContents()
        pasteboard.setString(executionPath, forType: .string)
    }

    var body: some View {
        Form {
            Section {
                // A custom row keeps the options left-aligned. SwiftUI's
                // `.radioGroup` picker is always laid out against the trailing
                // edge of a grouped Form row, which made the options look as if
                // they were laid out right-to-left.
                HStack(spacing: 22) {
                    ForEach(RoutingMode.allCases, id: \.self) { mode in
                        routingModeButton(for: mode)
                    }
                    Spacer(minLength: 0)
                }

                Text("settings.routing.footer")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            } header: {
                Text("settings.routing.header")
            }

            Section {
                Toggle(
                    "settings.launchAtLogin",
                    isOn: Binding(
                        get: { loginItem.isEnabled },
                        set: { loginItem.setEnabled($0) }
                    )
                )

                if loginItem.requiresApproval {
                    Text("settings.launchAtLogin.requiresApproval")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Button("settings.launchAtLogin.open") {
                        loginItem.openLoginItemsSettings()
                    }
                }

                if let errorMessage = loginItem.errorMessage {
                    Text(errorMessage)
                        .font(.caption)
                        .foregroundStyle(.red)
                }
            } header: {
                Text("settings.general.header")
            }

            Section {
                HStack(alignment: .top) {
                    VStack(alignment: .leading, spacing: 2) {
                        Text("settings.permissions.accessibility")
                        Text("settings.permissions.accessibility.description")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                    Spacer()
                    Text(permissions.accessibilityGranted
                         ? L10n.text("settings.permissions.granted")
                         : L10n.text("settings.permissions.notGranted"))
                        .foregroundStyle(permissions.accessibilityGranted ? .green : .orange)
                }

                HStack {
                    Button("permission.recheck") {
                        onRecheckAccessibility()
                    }
                    Button("settings.permissions.openAccessibility") {
                        permissions.openAccessibilitySettings()
                    }
                }

                ForEach(PlaybackTarget.allCases, id: \.self) { target in
                    HStack {
                        Text(verbatim: L10n.targetName(target))
                        Spacer()
                        automationBadge(for: target)
                    }
                }

                HStack {
                    Button("permission.automation.checkMusic") {
                        onCheckMusicAutomation()
                    }
                    Button("settings.permissions.openAutomation") {
                        permissions.openAutomationSettings()
                    }
                }

                Text("permission.automation.checkNote")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                Text("permission.automation.spotifyNote")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            } header: {
                Text("settings.permissions.header")
            }

            Section {
                Button {
                    withAnimation(.easeInOut(duration: 0.15)) {
                        diagnosticsExpanded.toggle()
                    }
                } label: {
                    HStack(spacing: 8) {
                        Image(systemName: "chevron.right")
                            .font(.caption.weight(.semibold))
                            .foregroundStyle(.secondary)
                            .rotationEffect(.degrees(diagnosticsExpanded ? 90 : 0))
                        Text("diagnostics.heading")
                            .foregroundStyle(.primary)
                        Spacer(minLength: 0)
                    }
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .contentShape(Rectangle())
                }
                .buttonStyle(.plain)

                if diagnosticsExpanded {
                    LabeledContent("diagnostics.path") {
                        HStack(spacing: 6) {
                            Text(verbatim: abbreviatedExecutionPath)
                                .font(.caption.monospaced())
                                .textSelection(.enabled)

                            Button {
                                copyExecutionPath()
                            } label: {
                                Image(systemName: "doc.on.doc")
                            }
                            .buttonStyle(.borderless)
                            .help(Text("diagnostics.path.copy"))
                            .accessibilityLabel(Text("diagnostics.path.copy"))
                        }
                    }

                    HStack {
                        Text("diagnostics.tap")
                        Spacer()
                        Text(permissions.eventTapActive
                             ? L10n.text("diagnostics.tap.active")
                             : L10n.text("diagnostics.tap.inactive"))
                            .foregroundStyle(permissions.eventTapActive ? .green : .secondary)
                    }
                }
            }

            Section {
                Text(L10n.format("settings.version", version))
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .formStyle(.grouped)
        .frame(width: 520, height: 720)
    }

    @ViewBuilder
    private func routingModeButton(for mode: RoutingMode) -> some View {
        let isSelected = settings.mode == mode

        Button {
            settings.mode = mode
        } label: {
            HStack(spacing: 8) {
                ZStack {
                    Circle()
                        .fill(isSelected ? Color.accentColor : Color(nsColor: .tertiaryLabelColor))
                        .frame(width: 16, height: 16)

                    if isSelected {
                        Circle()
                            .fill(.white)
                            .frame(width: 6, height: 6)
                    }
                }

                Text(verbatim: L10n.modeName(mode))
            }
            .contentShape(Rectangle())
        }
        .buttonStyle(.plain)
        .accessibilityLabel(Text(verbatim: L10n.modeName(mode)))
        .accessibilityAddTraits(isSelected ? .isSelected : [])
    }

    @ViewBuilder
    private func automationBadge(for target: PlaybackTarget) -> some View {
        switch permissions.automationStatus(for: target) {
        case .granted:
            Text("settings.permissions.granted")
                .foregroundStyle(.green)
        case .denied:
            Text("permission.automation.deniedShort")
                .foregroundStyle(.red)
        case .notChecked:
            Text("permission.automation.notChecked")
                .foregroundStyle(.secondary)
        }
    }
}

struct PermissionGuidanceView: View {
    @ObservedObject var permissions: PermissionManager

    let onRecheckAccessibility: () -> Void
    let onCheckMusicAutomation: () -> Void
    let onClose: () -> Void

    private var executionPath: String {
        Bundle.main.bundlePath
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("permission.title")
                .font(.title2)
                .fontWeight(.semibold)

            Text("permission.intro")
                .fixedSize(horizontal: false, vertical: true)

            GroupBox {
                VStack(alignment: .leading, spacing: 8) {
                    HStack {
                        Text("permission.accessibility.heading")
                            .fontWeight(.semibold)
                        Spacer()
                        Text(permissions.accessibilityGranted
                             ? L10n.text("settings.permissions.granted")
                             : L10n.text("settings.permissions.notGranted"))
                            .foregroundStyle(permissions.accessibilityGranted ? .green : .orange)
                    }

                    Text("permission.accessibility.body")
                        .font(.callout)
                        .foregroundStyle(.secondary)
                        .fixedSize(horizontal: false, vertical: true)

                    Text(L10n.format("permission.buildHint", executionPath))
                        .font(.caption.monospaced())
                        .foregroundStyle(.secondary)
                        .textSelection(.enabled)
                        .fixedSize(horizontal: false, vertical: true)

                    HStack {
                        if !permissions.accessibilityGranted {
                            Button("permission.requestAccessibility") {
                                permissions.requestAccessibility()
                            }
                        }
                        Button("permission.recheck") {
                            onRecheckAccessibility()
                        }
                        Button("permission.openAccessibility") {
                            permissions.openAccessibilitySettings()
                        }
                    }
                }
                .padding(6)
            }

            GroupBox {
                VStack(alignment: .leading, spacing: 8) {
                    Text("permission.automation.heading")
                        .fontWeight(.semibold)

                    Text("permission.automation.body")
                        .font(.callout)
                        .foregroundStyle(.secondary)
                        .fixedSize(horizontal: false, vertical: true)

                    ForEach(PlaybackTarget.allCases, id: \.self) { target in
                        HStack {
                            Text(verbatim: L10n.targetName(target))
                            Spacer()
                            automationBadge(for: target)
                        }
                    }

                    HStack {
                        Button("permission.automation.checkMusic") {
                            onCheckMusicAutomation()
                        }
                        Button("permission.openAutomation") {
                            permissions.openAutomationSettings()
                        }
                    }

                    Text("permission.automation.checkNote")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .fixedSize(horizontal: false, vertical: true)
                    Text("permission.automation.spotifyNote")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .fixedSize(horizontal: false, vertical: true)
                }
                .padding(6)
            }

            Text("permission.restartHint")
                .font(.caption)
                .foregroundStyle(.secondary)
                .fixedSize(horizontal: false, vertical: true)

            HStack {
                Spacer()
                Button("permission.close", action: onClose)
                    .keyboardShortcut(.cancelAction)
            }
        }
        .padding(24)
        .frame(width: 560)
    }

    @ViewBuilder
    private func automationBadge(for target: PlaybackTarget) -> some View {
        switch permissions.automationStatus(for: target) {
        case .granted:
            Text("settings.permissions.granted")
                .foregroundStyle(.green)
        case .denied:
            Text("permission.automation.deniedShort")
                .foregroundStyle(.red)
        case .notChecked:
            Text("permission.automation.notChecked")
                .foregroundStyle(.secondary)
        }
    }
}
