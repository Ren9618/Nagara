import Foundation

/// Builds the AppleScript sources used to control players.
/// Kept separate from execution so the generated commands can be asserted in
/// unit tests.
enum PlayerScriptFactory {
    static func profile(for target: PlaybackTarget) -> PlayerCommandProfile {
        .preset(target)
    }

    /// The dictionary command name to use for an action. When an app only
    /// exposes separate `play` and `pause` commands, the current state decides
    /// which one to send.
    static func commandName(
        for action: MediaKeyAction,
        profile: PlayerCommandProfile,
        state: PlayerState = .unknown
    ) -> String? {
        switch action {
        case .playPause:
            if let playPauseCommand = profile.playPauseCommand {
                return playPauseCommand
            }
            guard
                profile.supportsStateQuery,
                let play = profile.playCommand,
                let pause = profile.pauseCommand
            else {
                return nil
            }
            return state == .playing ? pause : play

        case .nextTrack:
            return profile.nextTrackCommand

        case .previousTrack:
            return profile.previousTrackCommand
        }
    }

    static func tellScript(command: String, bundleIdentifier: String) -> String {
        "tell application id \"\(escaped(bundleIdentifier))\" to \(command)"
    }

    static func commandScript(
        for action: MediaKeyAction,
        profile: PlayerCommandProfile,
        state: PlayerState = .unknown,
        bundleIdentifier: String
    ) -> String? {
        guard let command = commandName(for: action, profile: profile, state: state) else {
            return nil
        }
        return tellScript(command: command, bundleIdentifier: bundleIdentifier)
    }

    /// Script used for the first play/pause press when the app is not running.
    /// `play` launches the player and starts playback. Apps that only expose
    /// `playpause` fall back to it.
    static func playScript(
        for profile: PlayerCommandProfile,
        bundleIdentifier: String
    ) -> String? {
        guard let command = profile.playCommand ?? profile.playPauseCommand else {
            return nil
        }
        return tellScript(command: command, bundleIdentifier: bundleIdentifier)
    }

    static func stateScript(
        for profile: PlayerCommandProfile,
        bundleIdentifier: String
    ) -> String? {
        guard let command = profile.stateQueryCommand else {
            return nil
        }
        return tellScript(command: command, bundleIdentifier: bundleIdentifier)
    }

    // MARK: - Preset convenience (keeps v0.1 scripts stable)

    static func commandScript(for action: MediaKeyAction, target: PlaybackTarget) -> String {
        let profile = profile(for: target)
        return commandScript(
            for: action,
            profile: profile,
            bundleIdentifier: target.bundleIdentifier
        )!
    }

    static func playScript(for target: PlaybackTarget) -> String {
        let profile = profile(for: target)
        return playScript(for: profile, bundleIdentifier: target.bundleIdentifier)!
    }

    static func stateScript(for target: PlaybackTarget) -> String {
        let profile = profile(for: target)
        return stateScript(for: profile, bundleIdentifier: target.bundleIdentifier)!
    }

    private static func escaped(_ value: String) -> String {
        value
            .replacingOccurrences(of: "\\", with: "\\\\")
            .replacingOccurrences(of: "\"", with: "\\\"")
    }
}
