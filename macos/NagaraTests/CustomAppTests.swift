import XCTest

final class CustomAppTests: XCTestCase {
    private let profile = PlayerCommandProfile(
        playPauseCommand: "playpause",
        playCommand: "play",
        pauseCommand: "pause",
        nextTrackCommand: "next track",
        previousTrackCommand: "previous track",
        stateQuery: .playerState
    )

    private func makeApp(bundleIdentifier: String = "com.example.Player") -> CustomPlayerApp {
        CustomPlayerApp(
            bundleIdentifier: bundleIdentifier,
            displayName: "Example Player",
            profile: profile
        )
    }

    func testProfileReportsAllMissingRequirements() {
        let empty = PlayerCommandProfile(
            playPauseCommand: nil,
            playCommand: nil,
            pauseCommand: nil,
            nextTrackCommand: nil,
            previousTrackCommand: nil,
            stateQuery: nil
        )

        XCTAssertFalse(empty.isSupported)
        XCTAssertEqual(
            empty.missingRequirements,
            [.playPause, .nextTrack, .previousTrack]
        )
    }

    func testStateUnavailableProfileIsStillEligible() {
        let stateless = PlayerCommandProfile(
            playPauseCommand: "playpause",
            playCommand: "play",
            pauseCommand: "pause",
            nextTrackCommand: "next track",
            previousTrackCommand: "previous track",
            stateQuery: nil
        )

        XCTAssertTrue(stateless.isSupported)
        XCTAssertFalse(stateless.supportsStateQuery)
    }

    func testRoutingModePersistsOnlyTheCustomBundleIdentifier() {
        let app = makeApp()
        let mode = RoutingMode.custom(app)

        XCTAssertEqual(mode.persistedValue, "custom:com.example.Player")
        XCTAssertEqual(
            RoutingMode.fromPersistedValue(mode.persistedValue, customApps: [app]),
            mode
        )
    }

    func testCustomModeFallsBackWhenTheAppIsGone() {
        let app = makeApp()

        XCTAssertNil(
            RoutingMode.fromPersistedValue(
                RoutingMode.custom(app).persistedValue,
                customApps: []
            )
        )
    }

    func testBuiltInModesNeverContainCustomApps() {
        XCTAssertEqual(RoutingMode.allCases, [.spotify, .appleMusic, .automatic])
        XCTAssertTrue(RoutingMode.allCases.allSatisfy { $0.customApp == nil })
        XCTAssertTrue(RoutingMode.allCases.allSatisfy { $0.fixedDestination == nil || $0.fixedDestination?.isCustom == false })
    }

    func testCustomFixedDestinationCarriesTheApp() {
        let app = makeApp()

        XCTAssertEqual(RoutingMode.custom(app).fixedDestination, .custom(app))
        XCTAssertEqual(RoutingMode.custom(app).fixedDestination?.bundleIdentifier, app.bundleIdentifier)
    }

    func testPlaybackDestinationIdentifiers() {
        let app = makeApp()

        XCTAssertEqual(PlaybackDestination.spotify.bundleIdentifier, "com.spotify.client")
        XCTAssertEqual(PlaybackDestination.appleMusic.bundleIdentifier, "com.apple.Music")
        XCTAssertEqual(PlaybackDestination.custom(app).bundleIdentifier, app.bundleIdentifier)
        XCTAssertEqual(PlaybackDestination.custom(app).displayName, app.displayName)
        XCTAssertTrue(PlaybackDestination.custom(app).isCustom)
        XCTAssertFalse(PlaybackDestination.spotify.isCustom)
    }
}

private extension PlaybackDestination {
    var displayName: String {
        switch self {
        case .preset(let target):
            return target.rawValue
        case .custom(let app):
            return app.displayName
        }
    }
}
