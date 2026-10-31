import Foundation

/// Routes a media key action to exactly one player and remembers the last
/// built-in player that was playing (or was explicitly asked to play).
///
/// Automatic mode only considers the built-in presets. Custom apps are
/// fixed-target only in v0.2, so they never enter the `lastPlaying` history
/// used by automatic routing.
final class MediaKeyRouter {
    private let controller: PlayerControlling
    private let engine: RoutingEngine

    private(set) var mode: RoutingMode
    private(set) var lastPlaying: PlaybackTarget?

    /// Called whenever `lastPlaying` changes so the app can persist it.
    var onLastPlayingChange: ((PlaybackTarget?) -> Void)?

    init(
        controller: PlayerControlling,
        engine: RoutingEngine = RoutingEngine(),
        mode: RoutingMode = .automatic,
        lastPlaying: PlaybackTarget? = nil
    ) {
        self.controller = controller
        self.engine = engine
        self.mode = mode
        self.lastPlaying = lastPlaying
    }

    func updateMode(_ mode: RoutingMode) {
        self.mode = mode
    }

    func updateLastPlaying(_ target: PlaybackTarget?) {
        guard lastPlaying != target else {
            return
        }
        lastPlaying = target
        onLastPlayingChange?(target)
    }

    /// Resolves the destination and sends the command. Exactly one player is
    /// ever asked to perform an action.
    func handle(_ action: MediaKeyAction) {
        let destination: PlaybackDestination
        let targetState: PlayerState

        switch mode {
        case .automatic:
            var states: [PlaybackTarget: PlayerState] = [:]
            for candidate in PlaybackTarget.allCases {
                states[candidate] = controller.playerState(of: .preset(candidate))
            }
            let target = engine.target(for: mode, states: states, lastPlaying: lastPlaying)
            destination = .preset(target)
            targetState = states[target] ?? .unknown

        default:
            guard let fixedDestination = mode.fixedDestination else {
                return
            }
            destination = fixedDestination
            targetState = controller.playerState(of: destination)
        }

        controller.send(action, to: destination)

        // A play/pause command is treated as "this is the player the user is
        // using now" even if it pauses. Other commands only update the memory
        // while the player is actually playing. Only built-in players take
        // part in automatic routing, so custom destinations are excluded.
        guard let preset = destination.presetTarget else {
            return
        }
        if targetState == .playing || action == .playPause {
            updateLastPlaying(preset)
        }
    }
}
