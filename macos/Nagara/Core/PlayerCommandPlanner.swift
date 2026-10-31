import Foundation

/// The concrete action the AppleScript controller should take for one key
/// press. Kept separate from execution so the not-running / unknown-state
/// rules can be tested.
enum PlaybackCommandPlan: Equatable {
    case none
    case send(String)
    case launchOnly
}

enum PlayerCommandPlanner {
    static func plan(
        action: MediaKeyAction,
        profile: PlayerCommandProfile,
        isRunning: Bool,
        state: PlayerState = .unknown
    ) -> PlaybackCommandPlan {
        guard profile.isSupported else {
            return .none
        }

        guard isRunning else {
            guard action == .playPause else {
                return .none
            }

            // Apps whose state cannot be read must not be forced into
            // playback. Launching the app is enough for the first press.
            guard profile.supportsStateQuery else {
                return .launchOnly
            }

            if let command = profile.playCommand ?? profile.playPauseCommand {
                return .send(command)
            }
            return .none
        }

        guard let command = PlayerScriptFactory.commandName(
            for: action,
            profile: profile,
            state: state
        ) else {
            return .none
        }
        return .send(command)
    }
}
