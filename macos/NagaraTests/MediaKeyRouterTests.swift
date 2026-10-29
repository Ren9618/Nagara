import XCTest

final class MediaKeyRouterTests: XCTestCase {
    private final class MockPlayerController: PlayerControlling {
        struct Command: Equatable {
            let action: MediaKeyAction
            let target: PlaybackTarget
        }

        var states: [PlaybackTarget: PlayerState] = [:]
        private(set) var stateQueries: [PlaybackTarget] = []
        private(set) var commands: [Command] = []

        func playerState(of target: PlaybackTarget) -> PlayerState {
            stateQueries.append(target)
            return states[target] ?? .unknown
        }

        func send(_ action: MediaKeyAction, to target: PlaybackTarget) {
            commands.append(Command(action: action, target: target))
        }
    }

    private var controller: MockPlayerController!
    private var router: MediaKeyRouter!

    override func setUp() {
        super.setUp()
        controller = MockPlayerController()
        router = MediaKeyRouter(controller: controller, mode: .automatic)
    }

    func testFixedSpotifySendsOnlyToSpotify() {
        controller.states = [.spotify: .stopped, .appleMusic: .playing]
        router.updateMode(.spotify)

        router.handle(.nextTrack)

        XCTAssertEqual(controller.commands, [.init(action: .nextTrack, target: .spotify)])
        XCTAssertEqual(controller.stateQueries, [.spotify])
    }

    func testFixedAppleMusicSendsOnlyToAppleMusic() {
        controller.states = [.spotify: .playing, .appleMusic: .paused]
        router.updateMode(.appleMusic)

        router.handle(.previousTrack)

        XCTAssertEqual(controller.commands, [.init(action: .previousTrack, target: .appleMusic)])
        XCTAssertEqual(controller.stateQueries, [.appleMusic])
    }

    func testAutomaticSendsOnlyToThePlayingPlayer() {
        controller.states = [.spotify: .paused, .appleMusic: .playing]

        router.handle(.playPause)

        XCTAssertEqual(controller.commands, [.init(action: .playPause, target: .appleMusic)])
        XCTAssertEqual(Set(controller.stateQueries), Set([.spotify, .appleMusic]))
    }

    func testAutomaticFallsBackToLastPlayingWhenNothingPlays() {
        router.updateLastPlaying(.appleMusic)
        controller.states = [.spotify: .stopped, .appleMusic: .paused]

        router.handle(.nextTrack)

        XCTAssertEqual(controller.commands, [.init(action: .nextTrack, target: .appleMusic)])
    }

    func testAutomaticFallsBackToSpotifyWhenNothingPlaysAndHistoryIsUnknown() {
        controller.states = [.spotify: .notRunning, .appleMusic: .stopped]

        router.handle(.nextTrack)

        XCTAssertEqual(controller.commands, [.init(action: .nextTrack, target: .spotify)])
    }

    func testAutomaticUsesLastPlayingWhenBothPlay() {
        router.updateLastPlaying(.appleMusic)
        controller.states = [.spotify: .playing, .appleMusic: .playing]

        router.handle(.playPause)

        XCTAssertEqual(controller.commands, [.init(action: .playPause, target: .appleMusic)])
    }

    func testAutomaticUsesSpotifyWhenBothPlayAndHistoryIsUnknown() {
        controller.states = [.spotify: .playing, .appleMusic: .playing]

        router.handle(.playPause)

        XCTAssertEqual(controller.commands, [.init(action: .playPause, target: .spotify)])
    }

    func testPlayPauseUpdatesLastPlayingEvenWhenPlayerIsNotRunning() {
        router.updateMode(.spotify)
        controller.states = [.spotify: .notRunning]

        router.handle(.playPause)

        XCTAssertEqual(router.lastPlaying, .spotify)
    }

    func testNextTrackDoesNotUpdateLastPlayingWhenPlayerIsPaused() {
        router.updateMode(.spotify)
        router.updateLastPlaying(.appleMusic)
        controller.states = [.spotify: .paused]

        router.handle(.nextTrack)

        XCTAssertEqual(router.lastPlaying, .appleMusic)
    }

    func testNextTrackUpdatesLastPlayingWhilePlayerIsPlaying() {
        router.updateMode(.appleMusic)
        controller.states = [.appleMusic: .playing]

        router.handle(.nextTrack)

        XCTAssertEqual(router.lastPlaying, .appleMusic)
    }

    func testLastPlayingChangeIsReportedOnce() {
        var changes: [PlaybackTarget?] = []
        router.onLastPlayingChange = { changes.append($0) }
        router.updateMode(.spotify)
        controller.states = [.spotify: .playing]

        router.handle(.nextTrack)
        router.handle(.nextTrack)

        XCTAssertEqual(changes, [.spotify])
    }

    func testAutomaticRemembersPlayingPlayerAfterCommand() {
        controller.states = [.spotify: .paused, .appleMusic: .playing]
        router.handle(.nextTrack)
        XCTAssertEqual(router.lastPlaying, .appleMusic)

        controller.states = [.spotify: .stopped, .appleMusic: .paused]
        router.handle(.nextTrack)
        XCTAssertEqual(router.lastPlaying, .appleMusic)
        XCTAssertEqual(controller.commands.last, .init(action: .nextTrack, target: .appleMusic))
    }
}
