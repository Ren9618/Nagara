import Foundation

/// A user-added app that Nagara controls through its AppleScript dictionary.
///
/// Only the bundle identifier is used as the identity; the display name and
/// detected command profile are stored alongside it so the app can be restored
/// without inspecting the bundle again at launch.
struct CustomPlayerApp: Codable, Hashable, Identifiable {
    let bundleIdentifier: String
    let displayName: String
    let profile: PlayerCommandProfile

    var id: String {
        bundleIdentifier
    }
}

/// A target for a media key command: one of the built-in players or a
/// user-added AppleScript-compatible app.
enum PlaybackDestination: Hashable, Identifiable {
    case preset(PlaybackTarget)
    case custom(CustomPlayerApp)

    static let spotify = PlaybackDestination.preset(.spotify)
    static let appleMusic = PlaybackDestination.preset(.appleMusic)

    var id: String {
        bundleIdentifier
    }

    var bundleIdentifier: String {
        switch self {
        case .preset(let target):
            return target.bundleIdentifier
        case .custom(let app):
            return app.bundleIdentifier
        }
    }

    var presetTarget: PlaybackTarget? {
        if case .preset(let target) = self {
            return target
        }
        return nil
    }

    var customApp: CustomPlayerApp? {
        if case .custom(let app) = self {
            return app
        }
        return nil
    }

    var isCustom: Bool {
        customApp != nil
    }
}
