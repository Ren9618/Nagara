import Foundation

/// Decides which player receives a media key command.
///
/// The engine is deliberately pure so the routing rules can be unit tested
/// without touching AppleScript or AppKit.
struct RoutingEngine {
    func target(
        for mode: RoutingMode,
        states: [PlaybackTarget: PlayerState],
        lastPlaying: PlaybackTarget?
    ) -> PlaybackTarget {
        if let fixedTarget = mode.fixedTarget {
            return fixedTarget
        }

        let playingTargets = PlaybackTarget.allCases.filter { states[$0] == .playing }

        if playingTargets.count == 1 {
            return playingTargets[0]
        }

        if playingTargets.count > 1 {
            // Both players are playing. Prefer the one that was playing last,
            // otherwise fall back to the documented Spotify default.
            if let lastPlaying, playingTargets.contains(lastPlaying) {
                return lastPlaying
            }
            return .spotify
        }

        // Nothing is playing: use the last player that was playing, then
        // Spotify as the final fallback.
        return lastPlaying ?? .spotify
    }
}
