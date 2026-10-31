import AppKit
import ApplicationServices
import Combine

/// Whether macOS allows Nagara to control a player.
enum AutomationPermissionStatus {
    case granted
    case denied
    case notChecked
}

/// Tracks Accessibility and Automation permission state and opens the
/// relevant panes in System Settings. The Accessibility system prompt is only
/// requested in response to an explicit user action.
@MainActor
final class PermissionManager: ObservableObject {
    @Published private(set) var accessibilityGranted: Bool
    @Published private(set) var eventTapActive = false
    @Published private(set) var automationDeniedBundleIDs: Set<String> = []
    @Published private(set) var automationGrantedBundleIDs: Set<String> = []

    var hasAutomationDenial: Bool {
        !automationDeniedBundleIDs.isEmpty
    }

    init() {
        accessibilityGranted = AXIsProcessTrusted()
        NSLog(
            "Nagara: launched from %@ (bundle=%@, accessibility=%@)",
            Bundle.main.bundlePath,
            Bundle.main.bundleIdentifier ?? "unknown",
            accessibilityGranted ? "granted" : "not granted"
        )
    }

    @discardableResult
    func refreshAccessibility() -> Bool {
        let granted = AXIsProcessTrusted()
        if accessibilityGranted != granted {
            accessibilityGranted = granted
            NSLog("Nagara: accessibility changed to %@", granted ? "granted" : "not granted")
        }
        return granted
    }

    /// Shows the macOS Accessibility prompt. Call only from a user action.
    func requestAccessibility() {
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        accessibilityGranted = AXIsProcessTrustedWithOptions(options)
        NSLog(
            "Nagara: accessibility prompt requested; now %@",
            accessibilityGranted ? "granted" : "not granted"
        )
    }

    func setEventTapActive(_ active: Bool) {
        if eventTapActive != active {
            eventTapActive = active
            NSLog("Nagara: event tap %@", active ? "active" : "inactive")
        }
    }

    /// Returns true when this is the first denial recorded for the bundle.
    @discardableResult
    func noteAutomationDenied(bundleIdentifier: String) -> Bool {
        automationGrantedBundleIDs.remove(bundleIdentifier)
        return automationDeniedBundleIDs.insert(bundleIdentifier).inserted
    }

    func noteAutomationSucceeded(bundleIdentifier: String) {
        automationDeniedBundleIDs.remove(bundleIdentifier)
        automationGrantedBundleIDs.insert(bundleIdentifier)
    }

    func automationStatus(for target: PlaybackTarget) -> AutomationPermissionStatus {
        automationStatus(forBundleIdentifier: target.bundleIdentifier)
    }

    func automationStatus(for destination: PlaybackDestination) -> AutomationPermissionStatus {
        automationStatus(forBundleIdentifier: destination.bundleIdentifier)
    }

    private func automationStatus(forBundleIdentifier bundleIdentifier: String) -> AutomationPermissionStatus {
        if automationGrantedBundleIDs.contains(bundleIdentifier) {
            return .granted
        }
        if automationDeniedBundleIDs.contains(bundleIdentifier) {
            return .denied
        }
        return .notChecked
    }

    func openAccessibilitySettings() {
        openSystemSettings("x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility")
    }

    func openAutomationSettings() {
        openSystemSettings("x-apple.systempreferences:com.apple.preference.security?Privacy_Automation")
    }

    private func openSystemSettings(_ urlString: String) {
        guard let url = URL(string: urlString) else {
            return
        }
        NSWorkspace.shared.open(url)
    }
}
