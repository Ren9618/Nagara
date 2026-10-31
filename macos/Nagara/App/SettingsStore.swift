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
        static let customApps = "nagara.customApps"
    }

    @Published var mode: RoutingMode {
        didSet {
            defaults.set(mode.persistedValue, forKey: Key.mode)
            onModeChange?(mode)
        }
    }

    @Published var interceptionEnabled: Bool {
        didSet {
            defaults.set(interceptionEnabled, forKey: Key.interceptionEnabled)
            onInterceptionChange?(interceptionEnabled)
        }
    }

    /// User-added apps, persisted by bundle identifier (plus the detected
    /// command profile and display name).
    @Published private(set) var customApps: [CustomPlayerApp] {
        didSet {
            persistCustomApps()
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

        let storedApps = Self.loadCustomApps(from: defaults)
        customApps = storedApps
        mode = defaults.string(forKey: Key.mode)
            .flatMap { RoutingMode.fromPersistedValue($0, customApps: storedApps) } ?? .automatic
        interceptionEnabled = defaults.object(forKey: Key.interceptionEnabled) as? Bool ?? true
        lastPlaying = defaults.string(forKey: Key.lastPlaying)
            .flatMap(PlaybackTarget.init(rawValue:))
    }

    func setLastPlaying(_ target: PlaybackTarget?) {
        lastPlaying = target
    }

    func addCustomApp(_ app: CustomPlayerApp) {
        guard !customApps.contains(where: { $0.bundleIdentifier == app.bundleIdentifier }) else {
            return
        }
        customApps.append(app)
    }

    func removeCustomApp(_ app: CustomPlayerApp) {
        customApps.removeAll { $0.bundleIdentifier == app.bundleIdentifier }

        // Do not leave the router pointed at an app that no longer exists.
        if mode.customApp?.bundleIdentifier == app.bundleIdentifier {
            mode = .automatic
        }
    }

    private func persistCustomApps() {
        guard let data = try? JSONEncoder().encode(customApps) else {
            return
        }
        defaults.set(data, forKey: Key.customApps)
    }

    private static func loadCustomApps(from defaults: UserDefaults) -> [CustomPlayerApp] {
        guard
            let data = defaults.data(forKey: Key.customApps),
            let apps = try? JSONDecoder().decode([CustomPlayerApp].self, from: data)
        else {
            return []
        }
        return apps
    }
}
