import XCTest

final class PlayerScriptFactoryTests: XCTestCase {
    func testSpotifyPlayPauseScript() {
        XCTAssertEqual(
            PlayerScriptFactory.commandScript(for: .playPause, target: .spotify),
            "tell application id \"com.spotify.client\" to playpause"
        )
    }

    func testSpotifyNextAndPreviousScripts() {
        XCTAssertEqual(
            PlayerScriptFactory.commandScript(for: .nextTrack, target: .spotify),
            "tell application id \"com.spotify.client\" to next track"
        )
        XCTAssertEqual(
            PlayerScriptFactory.commandScript(for: .previousTrack, target: .spotify),
            "tell application id \"com.spotify.client\" to previous track"
        )
    }

    func testAppleMusicCommandScripts() {
        XCTAssertEqual(
            PlayerScriptFactory.commandScript(for: .playPause, target: .appleMusic),
            "tell application id \"com.apple.Music\" to playpause"
        )
        XCTAssertEqual(
            PlayerScriptFactory.commandScript(for: .nextTrack, target: .appleMusic),
            "tell application id \"com.apple.Music\" to next track"
        )
        XCTAssertEqual(
            PlayerScriptFactory.commandScript(for: .previousTrack, target: .appleMusic),
            "tell application id \"com.apple.Music\" to previous track"
        )
    }

    func testPlayScriptLaunchesAndStartsPlayback() {
        XCTAssertEqual(
            PlayerScriptFactory.playScript(for: .spotify),
            "tell application id \"com.spotify.client\" to play"
        )
        XCTAssertEqual(
            PlayerScriptFactory.playScript(for: .appleMusic),
            "tell application id \"com.apple.Music\" to play"
        )
    }

    func testStateScripts() {
        XCTAssertEqual(
            PlayerScriptFactory.stateScript(for: .spotify),
            "tell application id \"com.spotify.client\" to player state as string"
        )
        XCTAssertEqual(
            PlayerScriptFactory.stateScript(for: .appleMusic),
            "tell application id \"com.apple.Music\" to player state as string"
        )
    }

    func testBundleIdentifiers() {
        XCTAssertEqual(PlaybackTarget.spotify.bundleIdentifier, "com.spotify.client")
        XCTAssertEqual(PlaybackTarget.appleMusic.bundleIdentifier, "com.apple.Music")
    }

    func testCustomScriptsUseDetectedCommandNames() {
        let profile = PlayerCommandProfile(
            playPauseCommand: "playpause",
            playCommand: "play",
            pauseCommand: "pause",
            nextTrackCommand: "next",
            previousTrackCommand: "previous",
            stateQuery: .playing
        )

        XCTAssertEqual(
            PlayerScriptFactory.commandScript(
                for: .nextTrack,
                profile: profile,
                bundleIdentifier: "com.example.Player"
            ),
            "tell application id \"com.example.Player\" to next"
        )
        XCTAssertEqual(
            PlayerScriptFactory.stateScript(
                for: profile,
                bundleIdentifier: "com.example.Player"
            ),
            "tell application id \"com.example.Player\" to playing as string"
        )
    }

    func testCustomPlayPauseUsesSeparateCommandsWhenStateIsKnown() {
        let profile = PlayerCommandProfile(
            playPauseCommand: nil,
            playCommand: "play",
            pauseCommand: "pause",
            nextTrackCommand: "next track",
            previousTrackCommand: "previous track",
            stateQuery: .playerState
        )

        XCTAssertEqual(
            PlayerScriptFactory.commandScript(
                for: .playPause,
                profile: profile,
                state: .playing,
                bundleIdentifier: "com.example.Player"
            ),
            "tell application id \"com.example.Player\" to pause"
        )
        XCTAssertEqual(
            PlayerScriptFactory.commandScript(
                for: .playPause,
                profile: profile,
                state: .paused,
                bundleIdentifier: "com.example.Player"
            ),
            "tell application id \"com.example.Player\" to play"
        )
    }

    func testCustomPlayScriptFallsBackToPlayPause() {
        let profile = PlayerCommandProfile(
            playPauseCommand: "playpause",
            playCommand: nil,
            pauseCommand: nil,
            nextTrackCommand: "next track",
            previousTrackCommand: "previous track",
            stateQuery: nil
        )

        XCTAssertEqual(
            PlayerScriptFactory.playScript(
                for: profile,
                bundleIdentifier: "com.example.Player"
            ),
            "tell application id \"com.example.Player\" to playpause"
        )
    }
}
