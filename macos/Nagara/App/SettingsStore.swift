import Combine
import Foundation

/// Local-only settings stored in UserDefaults. Nagara never performs network
/// communication or telemetry.
@MainActor
final class SettingsStore: ObservableObject {
    private enum Key {
        static let mode = "nagara.routingMode"
        static let interceptionEnabled = "nagara.interceptionEnabled"
        static let lastPlaying = "nagara.lastPlaying"
    }

    @Published var mode: RoutingMode {
        didSet {
            defaults.set(mode.rawValue, forKey: Key.mode)
            onModeChange?(mode)
        }
    }

    @Published var interceptionEnabled: Bool {
        didSet {
            defaults.set(interceptionEnabled, forKey: Key.interceptionEnabled)
            onInterceptionChange?(interceptionEnabled)
        }
    }

    /// Set by the app delegate to keep the router and menu in sync.
    var onModeChange: ((RoutingMode) -> Void)?
    var onInterceptionChange: ((Bool) -> Void)?

    private(set) var lastPlaying: PlaybackTarget? {
        didSet {
            if let lastPlaying {
                defaults.set(lastPlaying.rawValue, forKey: Key.lastPlaying)
            } else {
                defaults.removeObject(forKey: Key.lastPlaying)
            }
        }
    }

    private let defaults: UserDefaults

    init(defaults: UserDefaults = .standard) {
        self.defaults = defaults

        mode = defaults.string(forKey: Key.mode)
            .flatMap(RoutingMode.init(rawValue:)) ?? .automatic
        interceptionEnabled = defaults.object(forKey: Key.interceptionEnabled) as? Bool ?? true
        lastPlaying = defaults.string(forKey: Key.lastPlaying)
            .flatMap(PlaybackTarget.init(rawValue:))
    }

    func setLastPlaying(_ target: PlaybackTarget?) {
        lastPlaying = target
    }
}
