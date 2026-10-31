import Foundation

/// Thin wrapper around the String Catalog lookup so UI code never hard-codes
/// user-facing text.
enum L10n {
    static func text(_ key: String) -> String {
        NSLocalizedString(key, comment: "")
    }

    static func format(_ key: String, _ arguments: CVarArg...) -> String {
        String(format: NSLocalizedString(key, comment: ""), locale: Locale.current, arguments: arguments)
    }

    static func targetName(_ target: PlaybackTarget) -> String {
        text(target.localizationKey)
    }

    static func destinationName(_ destination: PlaybackDestination) -> String {
        switch destination {
        case .preset(let target):
            return targetName(target)
        case .custom(let app):
            return app.displayName
        }
    }

    static func modeName(_ mode: RoutingMode) -> String {
        if let key = mode.localizationKey {
            return text(key)
        }
        return mode.customApp?.displayName ?? ""
    }

    static func commandName(_ requirement: PlayerCommandRequirement) -> String {
        switch requirement {
        case .playPause:
            return text("command.playPause")
        case .nextTrack:
            return text("command.nextTrack")
        case .previousTrack:
            return text("command.previousTrack")
        }
    }
}
