import AppKit
import Foundation

/// Executes AppleScript against Spotify, Apple Music and user-added apps.
///
/// NSAppleScript is used in-process (rather than shelling out to osascript) so
/// macOS attributes the Automation permission to Nagara itself.
final class AppleScriptPlayerController: PlayerControlling {
    /// Called when macOS reports that Nagara is not allowed to send Apple
    /// events to the player (error -1743).
    var onAutomationDenied: ((PlaybackDestination) -> Void)?
    var onAutomationSucceeded: ((PlaybackDestination) -> Void)?

    func playerState(of destination: PlaybackDestination) -> PlayerState {
        guard let profile = profile(for: destination) else {
            return .unknown
        }

        // Apps without a readable state are always reported as unknown. They
        // can still be used as fixed targets.
        guard profile.supportsStateQuery else {
            return .unknown
        }

        guard isRunning(destination) else {
            return .notRunning
        }

        guard
            let source = PlayerScriptFactory.stateScript(
                for: profile,
                bundleIdentifier: destination.bundleIdentifier
            ),
            let value = execute(source, destination: destination)
        else {
            return .unknown
        }

        let normalized = value
            .trimmingCharacters(in: .whitespacesAndNewlines)
            .lowercased()

        guard let stateQuery = profile.stateQuery else {
            return .unknown
        }

        switch stateQuery {
        case .playerState, .playerStatus:
            switch normalized {
            case "playing":
                return .playing
            case "paused":
                return .paused
            case "stopped":
                return .stopped
            default:
                return .unknown
            }
        case .playing:
            switch normalized {
            case "true", "1", "yes":
                return .playing
            case "false", "0", "no":
                return .paused
            default:
                return .unknown
            }
        }
    }

    func send(_ action: MediaKeyAction, to destination: PlaybackDestination) {
        guard let profile = profile(for: destination) else {
            return
        }

        let running = isRunning(destination)
        var state: PlayerState = .unknown
        if running, action == .playPause, profile.requiresStateForPlayPause {
            state = playerState(of: destination)
        }

        switch PlayerCommandPlanner.plan(
            action: action,
            profile: profile,
            isRunning: running,
            state: state
        ) {
        case .none:
            return

        case .send(let command):
            let source = PlayerScriptFactory.tellScript(
                command: command,
                bundleIdentifier: destination.bundleIdentifier
            )
            execute(source, destination: destination)

        case .launchOnly:
            launch(destination)
        }
    }

    /// Runs a harmless `player state` query. This triggers the macOS
    /// Automation prompt the first time and confirms whether Nagara may
    /// control the player. It never starts playback.
    func probeAutomation(for target: PlaybackTarget) {
        let destination = PlaybackDestination.preset(target)
        guard let source = PlayerScriptFactory.stateScript(
            for: .preset(target),
            bundleIdentifier: target.bundleIdentifier
        ) else {
            return
        }
        execute(source, destination: destination)
    }

    private func profile(for destination: PlaybackDestination) -> PlayerCommandProfile? {
        switch destination {
        case .preset(let target):
            return .preset(target)
        case .custom(let app):
            return app.profile
        }
    }

    private func isRunning(_ destination: PlaybackDestination) -> Bool {
        NSWorkspace.shared.runningApplications.contains {
            $0.bundleIdentifier == destination.bundleIdentifier
        }
    }

    /// Launches an app without sending it any Apple Events. Used for apps
    /// whose state is unavailable, so the first play key does not force
    /// playback to start.
    private func launch(_ destination: PlaybackDestination) {
        guard let url = NSWorkspace.shared.urlForApplication(
            withBundleIdentifier: destination.bundleIdentifier
        ) else {
            NSLog("Nagara: could not locate app %@", destination.bundleIdentifier)
            return
        }

        let configuration = NSWorkspace.OpenConfiguration()
        configuration.activates = true
        NSWorkspace.shared.openApplication(
            at: url,
            configuration: configuration
        ) { _, error in
            if let error {
                NSLog(
                    "Nagara: could not launch %@: %@",
                    destination.bundleIdentifier,
                    error.localizedDescription
                )
            }
        }
    }

    @discardableResult
    private func execute(_ source: String, destination: PlaybackDestination) -> String? {
        guard let script = NSAppleScript(source: source) else {
            return nil
        }

        var errorInfo: NSDictionary?
        let descriptor = script.executeAndReturnError(&errorInfo)

        if let errorInfo {
            let code = (errorInfo["NSAppleScriptErrorNumber"] as? NSNumber)?.intValue ?? 0
            if code == -1743 {
                onAutomationDenied?(destination)
            }
            let message = errorInfo["NSAppleScriptErrorMessage"] as? String ?? "unknown error"
            NSLog(
                "Nagara: AppleScript failed for %@ (code %d): %@",
                destination.bundleIdentifier,
                code,
                message
            )
            return nil
        }

        onAutomationSucceeded?(destination)
        return descriptor.stringValue
    }
}
