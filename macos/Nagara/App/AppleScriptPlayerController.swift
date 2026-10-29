import AppKit
import Foundation

/// Executes AppleScript against Spotify and Apple Music.
///
/// NSAppleScript is used in-process (rather than shelling out to osascript) so
/// macOS attributes the Automation permission to Nagara itself.
final class AppleScriptPlayerController: PlayerControlling {
    /// Called when macOS reports that Nagara is not allowed to send Apple
    /// events to the player (error -1743).
    var onAutomationDenied: ((PlaybackTarget) -> Void)?
    var onAutomationSucceeded: ((PlaybackTarget) -> Void)?

    func playerState(of target: PlaybackTarget) -> PlayerState {
        guard isRunning(target) else {
            return .notRunning
        }

        guard let value = execute(PlayerScriptFactory.stateScript(for: target), target: target) else {
            return .unknown
        }

        switch value.trimmingCharacters(in: .whitespacesAndNewlines).lowercased() {
        case "playing":
            return .playing
        case "paused":
            return .paused
        case "stopped":
            return .stopped
        default:
            return .unknown
        }
    }

    func send(_ action: MediaKeyAction, to target: PlaybackTarget) {
        if isRunning(target) {
            execute(PlayerScriptFactory.commandScript(for: action, target: target), target: target)
        } else if action == .playPause {
            // The app is not running: `play` launches it and starts playback.
            execute(PlayerScriptFactory.playScript(for: target), target: target)
        }
    }

    /// Runs a harmless `player state` query. This triggers the macOS
    /// Automation prompt the first time and confirms whether Nagara may
    /// control the player. It never starts playback.
    func probeAutomation(for target: PlaybackTarget) {
        execute(PlayerScriptFactory.stateScript(for: target), target: target)
    }

    private func isRunning(_ target: PlaybackTarget) -> Bool {
        NSWorkspace.shared.runningApplications.contains {
            $0.bundleIdentifier == target.bundleIdentifier
        }
    }

    @discardableResult
    private func execute(_ source: String, target: PlaybackTarget) -> String? {
        guard let script = NSAppleScript(source: source) else {
            return nil
        }

        var errorInfo: NSDictionary?
        let descriptor = script.executeAndReturnError(&errorInfo)

        if let errorInfo {
            let code = (errorInfo["NSAppleScriptErrorNumber"] as? NSNumber)?.intValue ?? 0
            if code == -1743 {
                onAutomationDenied?(target)
            }
            let message = errorInfo["NSAppleScriptErrorMessage"] as? String ?? "unknown error"
            NSLog("Nagara: AppleScript failed for %@ (code %d): %@", target.rawValue, code, message)
            return nil
        }

        onAutomationSucceeded?(target)
        return descriptor.stringValue
    }
}
