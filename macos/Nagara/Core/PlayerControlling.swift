import Foundation

/// Abstraction over the real AppleScript player controller so routing can be
/// tested with a mock.
protocol PlayerControlling: AnyObject {
    func playerState(of target: PlaybackTarget) -> PlayerState
    func send(_ action: MediaKeyAction, to target: PlaybackTarget)
}
