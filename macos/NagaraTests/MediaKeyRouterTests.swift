import XCTest

final class MediaKeyRouterTests: XCTestCase {
    private final class MockPlayerController: PlayerControlling {
        struct Command: Equatable {
            let action: MediaKeyAction
            let target: PlaybackDestination
        }

        var states: [PlaybackTarget: PlayerState] = [:]
        var destinationStates: [PlaybackDestination: PlayerState] = [:]
        private(set) var stateQueries: [PlaybackDestination] = []
        private(set) var commands: [Command] = []

        func playerState(of destination: PlaybackDestination) -> PlayerState {
            stateQueries.append(destination)
            if let preset = destination.presetTarget {
                return states[preset] ?? .unknown
            }
            return destinationStates[destination] ?? .unknown
        }

        func send(_ action: MediaKeyAction, to destination: PlaybackDestination) {
            commands.append(Command(action: action, target: destination))
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

    // MARK: - Custom apps (v0.2)

    private func makeCustomApp(stateAvailable: Bool) -> CustomPlayerApp {
        CustomPlayerApp(
            bundleIdentifier: "com.example.Player",
            displayName: "Example Player",
            profile: PlayerCommandProfile(
                playPauseCommand: "playpause",
                playCommand: "play",
                pauseCommand: "pause",
                nextTrackCommand: "next track",
                previousTrackCommand: "previous track",
                stateQuery: stateAvailable ? .playerState : nil
            )
        )
    }

    func testFixedCustomTargetSendsOnlyToTheCustomApp() {
        let app = makeCustomApp(stateAvailable: true)
        router.updateMode(.custom(app))
        controller.destinationStates[.custom(app)] = .playing

        router.handle(.nextTrack)

        XCTAssertEqual(controller.commands, [.init(action: .nextTrack, target: .custom(app))])
        XCTAssertEqual(controller.stateQueries, [.custom(app)])
    }

    func testCustomUnknownStateStillReceivesFixedCommands() {
        let app = makeCustomApp(stateAvailable: false)
        router.updateMode(.custom(app))
        controller.destinationStates[.custom(app)] = .unknown

        router.handle(.previousTrack)

        XCTAssertEqual(controller.commands, [.init(action: .previousTrack, target: .custom(app))])
    }

    func testCustomPlayPauseDoesNotChangeAutomaticHistory() {
        let app = makeCustomApp(stateAvailable: false)
        router.updateLastPlaying(.appleMusic)
        router.updateMode(.custom(app))
        controller.destinationStates[.custom(app)] = .unknown

        router.handle(.playPause)

        XCTAssertEqual(router.lastPlaying, .appleMusic)
    }

    func testAutomaticQueriesOnlyBuiltInPlayers() {
        controller.states = [.spotify: .playing, .appleMusic: .stopped]

        router.handle(.playPause)

        XCTAssertEqual(controller.stateQueries, [.spotify, .appleMusic])
        XCTAssertEqual(controller.commands, [.init(action: .playPause, target: .spotify)])
    }
}
