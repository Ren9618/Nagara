import XCTest

final class RoutingEngineTests: XCTestCase {
    private let engine = RoutingEngine()

    func testFixedSpotifyModeIgnoresPlayerStates() {
        let target = engine.target(
            for: .spotify,
            states: [.spotify: .stopped, .appleMusic: .playing],
            lastPlaying: .appleMusic
        )
        XCTAssertEqual(target, .spotify)
    }

    func testFixedAppleMusicModeIgnoresPlayerStates() {
        let target = engine.target(
            for: .appleMusic,
            states: [.spotify: .playing, .appleMusic: .stopped],
            lastPlaying: .spotify
        )
        XCTAssertEqual(target, .appleMusic)
    }

    func testAutomaticChoosesTheOnlyPlayingPlayer() {
        let target = engine.target(
            for: .automatic,
            states: [.spotify: .paused, .appleMusic: .playing],
            lastPlaying: .spotify
        )
        XCTAssertEqual(target, .appleMusic)
    }

    func testAutomaticPrefersLastPlayingWhenBothArePlaying() {
        let target = engine.target(
            for: .automatic,
            states: [.spotify: .playing, .appleMusic: .playing],
            lastPlaying: .appleMusic
        )
        XCTAssertEqual(target, .appleMusic)
    }

    func testAutomaticFallsBackToSpotifyWhenBothPlayAndHistoryIsUnknown() {
        let target = engine.target(
            for: .automatic,
            states: [.spotify: .playing, .appleMusic: .playing],
            lastPlaying: nil
        )
        XCTAssertEqual(target, .spotify)
    }

    func testAutomaticPrefersLastPlayingSpotifyWhenBothArePlaying() {
        let target = engine.target(
            for: .automatic,
            states: [.spotify: .playing, .appleMusic: .playing],
            lastPlaying: .spotify
        )
        XCTAssertEqual(target, .spotify)
    }

    func testAutomaticUsesLastPlayingWhenNothingIsPlaying() {
        let target = engine.target(
            for: .automatic,
            states: [.spotify: .stopped, .appleMusic: .paused],
            lastPlaying: .appleMusic
        )
        XCTAssertEqual(target, .appleMusic)
    }

    func testAutomaticDefaultsToSpotifyWhenNothingIsPlayingAndHistoryIsUnknown() {
        let target = engine.target(
            for: .automatic,
            states: [.spotify: .stopped, .appleMusic: .stopped],
            lastPlaying: nil
        )
        XCTAssertEqual(target, .spotify)
    }

    func testAutomaticTreatsNotRunningAndUnknownAsNotPlaying() {
        let target = engine.target(
            for: .automatic,
            states: [.spotify: .unknown, .appleMusic: .notRunning],
            lastPlaying: nil
        )
        XCTAssertEqual(target, .spotify)
    }

    func testAutomaticHandlesMissingStates() {
        let target = engine.target(
            for: .automatic,
            states: [:],
            lastPlaying: .appleMusic
        )
        XCTAssertEqual(target, .appleMusic)
    }

    func testAutomaticPrefersPlayingPlayerOverLastPlayingWhenOnlyOnePlays() {
        let target = engine.target(
            for: .automatic,
            states: [.spotify: .playing, .appleMusic: .paused],
            lastPlaying: .appleMusic
        )
        XCTAssertEqual(target, .spotify)
    }
}
