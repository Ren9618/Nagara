import XCTest

final class PlayerCommandPlannerTests: XCTestCase {
    private let statefulProfile = PlayerCommandProfile(
        playPauseCommand: "playpause",
        playCommand: "play",
        pauseCommand: "pause",
        nextTrackCommand: "next track",
        previousTrackCommand: "previous track",
        stateQuery: .playerState
    )

    private let statelessProfile = PlayerCommandProfile(
        playPauseCommand: "playpause",
        playCommand: "play",
        pauseCommand: "pause",
        nextTrackCommand: "next track",
        previousTrackCommand: "previous track",
        stateQuery: nil
    )

    func testPresetNotRunningPlayPauseSendsPlay() {
        let plan = PlayerCommandPlanner.plan(
            action: .playPause,
            profile: .standard,
            isRunning: false
        )

        XCTAssertEqual(plan, .send("play"))
    }

    func testStateCapableCustomNotRunningPlayPauseSendsPlay() {
        let plan = PlayerCommandPlanner.plan(
            action: .playPause,
            profile: statefulProfile,
            isRunning: false
        )

        XCTAssertEqual(plan, .send("play"))
    }

    func testStateUnavailableCustomNotRunningPlayPauseOnlyLaunches() {
        let plan = PlayerCommandPlanner.plan(
            action: .playPause,
            profile: statelessProfile,
            isRunning: false
        )

        XCTAssertEqual(plan, .launchOnly)
    }

    func testStateUnavailableCustomNotRunningNextTrackDoesNothing() {
        let plan = PlayerCommandPlanner.plan(
            action: .nextTrack,
            profile: statelessProfile,
            isRunning: false
        )

        XCTAssertEqual(plan, .none)
    }

    func testRunningCustomUsesPlayPauseCommand() {
        let plan = PlayerCommandPlanner.plan(
            action: .playPause,
            profile: statelessProfile,
            isRunning: true
        )

        XCTAssertEqual(plan, .send("playpause"))
    }

    func testPlayPausePairUsesPauseWhenStateIsPlaying() {
        let profile = PlayerCommandProfile(
            playPauseCommand: nil,
            playCommand: "play",
            pauseCommand: "pause",
            nextTrackCommand: "next track",
            previousTrackCommand: "previous track",
            stateQuery: .playerState
        )

        let plan = PlayerCommandPlanner.plan(
            action: .playPause,
            profile: profile,
            isRunning: true,
            state: .playing
        )

        XCTAssertEqual(plan, .send("pause"))
    }

    func testPlayPausePairUsesPlayWhenStateIsPaused() {
        let profile = PlayerCommandProfile(
            playPauseCommand: nil,
            playCommand: "play",
            pauseCommand: "pause",
            nextTrackCommand: "next track",
            previousTrackCommand: "previous track",
            stateQuery: .playerState
        )

        let plan = PlayerCommandPlanner.plan(
            action: .playPause,
            profile: profile,
            isRunning: true,
            state: .paused
        )

        XCTAssertEqual(plan, .send("play"))
    }

    func testUnknownStateStillSendsFixedNextTrack() {
        let plan = PlayerCommandPlanner.plan(
            action: .nextTrack,
            profile: statelessProfile,
            isRunning: true,
            state: .unknown
        )

        XCTAssertEqual(plan, .send("next track"))
    }

    func testUnsupportedProfileDoesNothing() {
        let profile = PlayerCommandProfile(
            playPauseCommand: "playpause",
            playCommand: nil,
            pauseCommand: nil,
            nextTrackCommand: nil,
            previousTrackCommand: "previous track",
            stateQuery: nil
        )

        XCTAssertEqual(
            PlayerCommandPlanner.plan(
                action: .playPause,
                profile: profile,
                isRunning: true
            ),
            .none
        )
    }
}
