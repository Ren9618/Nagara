import Foundation

/// The two media players Nagara can control.
enum PlaybackTarget: String, CaseIterable, Codable, Hashable {
    case spotify
    case appleMusic

    var bundleIdentifier: String {
        switch self {
        case .spotify:
            return "com.spotify.client"
        case .appleMusic:
            return "com.apple.Music"
        }
    }

    var localizationKey: String {
        switch self {
        case .spotify:
            return "target.spotify"
        case .appleMusic:
            return "target.appleMusic"
        }
    }
}

/// How media keys are routed.
enum RoutingMode: String, CaseIterable, Codable {
    case spotify
    case appleMusic
    case automatic

    /// The fixed target, or `nil` when the mode is automatic.
    var fixedTarget: PlaybackTarget? {
        switch self {
        case .spotify:
            return .spotify
        case .appleMusic:
            return .appleMusic
        case .automatic:
            return nil
        }
    }

    var localizationKey: String {
        switch self {
        case .spotify:
            return "mode.spotify"
        case .appleMusic:
            return "mode.appleMusic"
        case .automatic:
            return "mode.automatic"
        }
    }
}

/// The state returned by an AppleScript `player state` query.
enum PlayerState: String, Equatable {
    case playing
    case paused
    case stopped
    case notRunning
    case unknown

    var isPlaying: Bool {
        self == .playing
    }
}

/// The media key actions Nagara handles.
enum MediaKeyAction: Equatable {
    case playPause
    case nextTrack
    case previousTrack
}
