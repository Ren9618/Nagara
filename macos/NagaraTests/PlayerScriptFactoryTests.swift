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
}
