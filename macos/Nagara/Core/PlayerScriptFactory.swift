import Foundation

/// Builds the AppleScript sources used to control the supported players.
/// Kept separate from execution so the generated commands can be asserted in
/// unit tests.
enum PlayerScriptFactory {
    static func commandScript(for action: MediaKeyAction, target: PlaybackTarget) -> String {
        let command: String
        switch action {
        case .playPause:
            command = "playpause"
        case .nextTrack:
            command = "next track"
        case .previousTrack:
            command = "previous track"
        }
        return tellScript(command: command, target: target)
    }

    static func playScript(for target: PlaybackTarget) -> String {
        tellScript(command: "play", target: target)
    }

    static func stateScript(for target: PlaybackTarget) -> String {
        tellScript(command: "player state as string", target: target)
    }

    private static func tellScript(command: String, target: PlaybackTarget) -> String {
        "tell application id \"\(target.bundleIdentifier)\" to \(command)"
    }
}
