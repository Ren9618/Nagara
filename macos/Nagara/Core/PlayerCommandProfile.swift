import Foundation

/// The playback operations a custom app must provide to be usable as a
/// media-key target.
enum PlayerCommandRequirement: String, CaseIterable, Hashable {
    case playPause
    case nextTrack
    case previousTrack
}

/// Describes how a player exposes playback control through AppleScript.
///
/// The profile is intentionally data-only so support detection and command
/// selection can be unit tested without executing AppleScript.
struct PlayerCommandProfile: Codable, Equatable, Hashable {
    enum StateQuery: String, Codable, Hashable {
        case playerState
        case playerStatus
        case playing
    }

    var playPauseCommand: String?
    var playCommand: String?
    var pauseCommand: String?
    var nextTrackCommand: String?
    var previousTrackCommand: String?
    var stateQuery: StateQuery?

    /// Some apps expose `playpause`; others only expose separate `play` and
    /// `pause` commands. The latter can still toggle when state is readable.
    var supportsPlayPause: Bool {
        if playPauseCommand != nil {
            return true
        }
        return playCommand != nil && pauseCommand != nil && stateQuery != nil
    }

    var supportsNextTrack: Bool {
        nextTrackCommand != nil
    }

    var supportsPreviousTrack: Bool {
        previousTrackCommand != nil
    }

    var supportsStateQuery: Bool {
        stateQuery != nil
    }

    /// True when play/pause has to read the current state to choose between
    /// the separate `play` and `pause` commands.
    var requiresStateForPlayPause: Bool {
        playPauseCommand == nil && supportsPlayPause
    }

    var missingRequirements: Set<PlayerCommandRequirement> {
        var missing: Set<PlayerCommandRequirement> = []
        if !supportsPlayPause {
            missing.insert(.playPause)
        }
        if !supportsNextTrack {
            missing.insert(.nextTrack)
        }
        if !supportsPreviousTrack {
            missing.insert(.previousTrack)
        }
        return missing
    }

    var isSupported: Bool {
        missingRequirements.isEmpty
    }

    var stateQueryCommand: String? {
        switch stateQuery {
        case .playerState:
            return "player state as string"
        case .playerStatus:
            return "player status as string"
        case .playing:
            return "playing as string"
        case .none:
            return nil
        }
    }

    /// Spotify and Apple Music expose the same standard media command set.
    static let standard = PlayerCommandProfile(
        playPauseCommand: "playpause",
        playCommand: "play",
        pauseCommand: "pause",
        nextTrackCommand: "next track",
        previousTrackCommand: "previous track",
        stateQuery: .playerState
    )

    static func preset(_ target: PlaybackTarget) -> PlayerCommandProfile {
        switch target {
        case .spotify, .appleMusic:
            return .standard
        }
    }
}
