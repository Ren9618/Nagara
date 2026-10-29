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

    static func modeName(_ mode: RoutingMode) -> String {
        text(mode.localizationKey)
    }
}
