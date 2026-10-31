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
enum RoutingMode: Hashable {
    case spotify
    case appleMusic
    case automatic
    case custom(CustomPlayerApp)

    /// The three built-in modes. Custom apps are selected separately and are
    /// never candidates for automatic routing (a v0.2 decision).
    static let allCases: [RoutingMode] = [.spotify, .appleMusic, .automatic]

    /// The fixed destination, or `nil` when the mode is automatic.
    var fixedDestination: PlaybackDestination? {
        switch self {
        case .spotify:
            return .preset(.spotify)
        case .appleMusic:
            return .preset(.appleMusic)
        case .automatic:
            return nil
        case .custom(let app):
            return .custom(app)
        }
    }

    var localizationKey: String? {
        switch self {
        case .spotify:
            return "mode.spotify"
        case .appleMusic:
            return "mode.appleMusic"
        case .automatic:
            return "mode.automatic"
        case .custom:
            return nil
        }
    }

    var customApp: CustomPlayerApp? {
        if case .custom(let app) = self {
            return app
        }
        return nil
    }

    /// Stable value stored in UserDefaults. Custom modes persist only the
    /// bundle identifier and are resolved against the current app list.
    var persistedValue: String {
        switch self {
        case .spotify:
            return "spotify"
        case .appleMusic:
            return "appleMusic"
        case .automatic:
            return "automatic"
        case .custom(let app):
            return "custom:\(app.bundleIdentifier)"
        }
    }

    static func fromPersistedValue(
        _ value: String,
        customApps: [CustomPlayerApp]
    ) -> RoutingMode? {
        let customPrefix = "custom:"
        if value.hasPrefix(customPrefix) {
            let bundleIdentifier = String(value.dropFirst(customPrefix.count))
            guard let app = customApps.first(where: { $0.bundleIdentifier == bundleIdentifier }) else {
                return nil
            }
            return .custom(app)
        }

        switch value {
        case "spotify":
            return .spotify
        case "appleMusic":
            return .appleMusic
        case "automatic":
            return .automatic
        default:
            return nil
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
