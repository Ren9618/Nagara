import Foundation

/// Routes a media key action to exactly one player and remembers the last
/// player that was playing (or was explicitly asked to play).
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
        let target: PlaybackTarget
        let targetState: PlayerState

        switch mode {
        case .spotify, .appleMusic:
            guard let fixedTarget = mode.fixedTarget else {
                return
            }
            target = fixedTarget
            targetState = controller.playerState(of: target)

        case .automatic:
            var states: [PlaybackTarget: PlayerState] = [:]
            for candidate in PlaybackTarget.allCases {
                states[candidate] = controller.playerState(of: candidate)
            }
            target = engine.target(for: mode, states: states, lastPlaying: lastPlaying)
            targetState = states[target] ?? .unknown
        }

        controller.send(action, to: target)

        // A play/pause command is treated as "this is the player the user is
        // using now" even if it pauses. Other commands only update the memory
        // while the player is actually playing.
        if targetState == .playing || action == .playPause {
            updateLastPlaying(target)
        }
    }
}
