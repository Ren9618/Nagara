import XCTest

final class ScriptingDefinitionParserTests: XCTestCase {
    private func profile(_ xml: String) -> PlayerCommandProfile {
        let summary = ScriptingDefinitionParser.summary(from: Data(xml.utf8))
        return ScriptingDefinitionParser.profile(from: summary)
    }

    private func dictionary(commands: [String], properties: [String] = []) -> String {
        let commandXML = commands
            .map { "<command name=\"\($0)\"/>" }
            .joined(separator: "\n")
        let propertyXML = properties
            .map { "<property name=\"\($0)\"/>" }
            .joined(separator: "\n")
        return """
        <?xml version="1.0" encoding="UTF-8"?>
        <dictionary>
          <suite name="Test Suite">
            \(commandXML)
            <class name="application">
              \(propertyXML)
            </class>
          </suite>
        </dictionary>
        """
    }

    func testParsesStandardPlaybackCommandsAndPlayerState() {
        let profile = profile(dictionary(
            commands: ["playpause", "next track", "previous track", "play", "pause"],
            properties: ["player state"]
        ))

        XCTAssertEqual(profile.playPauseCommand, "playpause")
        XCTAssertEqual(profile.playCommand, "play")
        XCTAssertEqual(profile.pauseCommand, "pause")
        XCTAssertEqual(profile.nextTrackCommand, "next track")
        XCTAssertEqual(profile.previousTrackCommand, "previous track")
        XCTAssertEqual(profile.stateQuery, .playerState)
        XCTAssertTrue(profile.supportsStateQuery)
        XCTAssertTrue(profile.isSupported)
        XCTAssertFalse(profile.requiresStateForPlayPause)
    }

    func testParsesNextAndPreviousAliases() {
        let profile = profile(dictionary(
            commands: ["playpause", "next", "previous"],
            properties: ["player state"]
        ))

        XCTAssertEqual(profile.nextTrackCommand, "next")
        XCTAssertEqual(profile.previousTrackCommand, "previous")
        XCTAssertTrue(profile.isSupported)
    }

    func testDetectsPlayingBooleanAsStateSource() {
        let profile = profile(dictionary(
            commands: ["playpause", "next track", "previous track", "play", "pause"],
            properties: ["playing"]
        ))

        XCTAssertEqual(profile.stateQuery, .playing)
        XCTAssertTrue(profile.supportsStateQuery)
        XCTAssertTrue(profile.isSupported)
    }

    func testPlayerStateIsPreferredOverPlayingProperty() {
        let profile = profile(dictionary(
            commands: ["playpause", "next track", "previous track"],
            properties: ["playing", "player state"]
        ))

        XCTAssertEqual(profile.stateQuery, .playerState)
    }

    func testPlayAndPausePairCanReplacePlayPauseWhenStateIsReadable() {
        let profile = profile(dictionary(
            commands: ["play", "pause", "next track", "previous track"],
            properties: ["player state"]
        ))

        XCTAssertNil(profile.playPauseCommand)
        XCTAssertTrue(profile.supportsPlayPause)
        XCTAssertTrue(profile.requiresStateForPlayPause)
        XCTAssertTrue(profile.isSupported)
    }

    func testPlayAndPausePairWithoutStateIsUnsupported() {
        let profile = profile(dictionary(
            commands: ["play", "pause", "next track", "previous track"]
        ))

        XCTAssertFalse(profile.supportsPlayPause)
        XCTAssertFalse(profile.isSupported)
        XCTAssertTrue(profile.missingRequirements.contains(.playPause))
    }

    func testStateUnavailableAppIsStillSupportedForFixedMode() {
        let profile = profile(dictionary(
            commands: ["playpause", "next track", "previous track"]
        ))

        XCTAssertFalse(profile.supportsStateQuery)
        XCTAssertTrue(profile.isSupported)
        XCTAssertEqual(profile.missingRequirements, [])
    }

    func testMissingNextTrackIsReported() {
        let profile = profile(dictionary(
            commands: ["playpause", "previous track"],
            properties: ["player state"]
        ))

        XCTAssertFalse(profile.isSupported)
        XCTAssertEqual(profile.missingRequirements, [.nextTrack])
    }

    func testMissingPreviousTrackIsReported() {
        let profile = profile(dictionary(
            commands: ["playpause", "next track"],
            properties: ["player state"]
        ))

        XCTAssertFalse(profile.isSupported)
        XCTAssertEqual(profile.missingRequirements, [.previousTrack])
    }

    func testMissingAllPlaybackCommandsIsReported() {
        let profile = profile(dictionary(
            commands: ["open", "quit"],
            properties: ["name"]
        ))

        XCTAssertFalse(profile.isSupported)
        XCTAssertEqual(
            profile.missingRequirements,
            [.playPause, .nextTrack, .previousTrack]
        )
    }

    func testNormalizesCaseAndWhitespace() {
        let profile = profile(dictionary(
            commands: ["  PlayPause  ", "NEXT   TRACK", " previous track "],
            properties: ["  PLAYER   STATE "]
        ))

        XCTAssertEqual(profile.playPauseCommand, "playpause")
        XCTAssertEqual(profile.nextTrackCommand, "next track")
        XCTAssertEqual(profile.previousTrackCommand, "previous track")
        XCTAssertEqual(profile.stateQuery, .playerState)
        XCTAssertTrue(profile.isSupported)
    }

    func testEmptyDefinitionHasNoCommands() {
        let summary = ScriptingDefinitionParser.summary(from: Data())
        let profile = ScriptingDefinitionParser.profile(from: summary)

        XCTAssertEqual(summary, ScriptingDefinitionSummary())
        XCTAssertFalse(profile.isSupported)
        XCTAssertEqual(
            profile.missingRequirements,
            [.playPause, .nextTrack, .previousTrack]
        )
    }

    func testUnreadableXMLFallsBackToNoCommands() {
        let summary = ScriptingDefinitionParser.summary(
            from: Data("<dictionary><broken>".utf8)
        )
        let profile = ScriptingDefinitionParser.profile(from: summary)

        XCTAssertFalse(profile.isSupported)
    }

    // MARK: - Legacy .scriptSuite format (VLC and older apps)

    private func scriptSuite(commands: [String], attributes: [String]) -> Data {
        let commandValues = Dictionary(uniqueKeysWithValues: commands.map {
            ($0, ["CommandClass": "NSScriptCommand", "AppleEventCode": "TEST"] as [String: Any])
        })
        let attributeValues = Dictionary(uniqueKeysWithValues: attributes.map {
            ($0, ["Type": "NSNumber"] as [String: Any])
        })
        let dictionary: [String: Any] = [
            "Commands": commandValues,
            "Classes": [
                "Application": [
                    "Attributes": attributeValues,
                ],
            ],
        ]
        return try! PropertyListSerialization.data(
            fromPropertyList: dictionary,
            format: .binary,
            options: 0
        )
    }

    private func terminology(
        commands: [String: String],
        attributes: [String: String]
    ) -> Data {
        let commandValues = commands.mapValues { ["Name": $0] }
        let attributeValues = attributes.mapValues { ["Name": $0] }
        let dictionary: [String: Any] = [
            "Commands": commandValues,
            "Classes": [
                "Application": [
                    "Attributes": attributeValues,
                ],
            ],
        ]
        return try! PropertyListSerialization.data(
            fromPropertyList: dictionary,
            format: .binary,
            options: 0
        )
    }

    func testParsesLegacyScriptSuiteLikeVLC() {
        let summary = ScriptingDefinitionParser.summary(
            fromScriptSuite: scriptSuite(
                commands: ["play", "next", "previous"],
                attributes: ["playing"]
            ),
            terminologyData: terminology(
                commands: ["play": "play", "next": "next", "previous": "previous"],
                attributes: ["playing": "playing"]
            )
        )
        let profile = ScriptingDefinitionParser.profile(from: summary)

        // VLC's `play` command toggles playback, so it can stand in for
        // play/pause even though the dictionary has no `playpause`.
        XCTAssertEqual(profile.playPauseCommand, "play")
        XCTAssertEqual(profile.playCommand, "play")
        XCTAssertNil(profile.pauseCommand)
        XCTAssertEqual(profile.nextTrackCommand, "next")
        XCTAssertEqual(profile.previousTrackCommand, "previous")
        XCTAssertEqual(profile.stateQuery, .playing)
        XCTAssertTrue(profile.isSupported)
        XCTAssertFalse(profile.requiresStateForPlayPause)
    }

    func testLegacyScriptSuiteWithPlayAndPauseUsesStatefulPair() {
        let summary = ScriptingDefinitionParser.summary(
            fromScriptSuite: scriptSuite(
                commands: ["play", "pause", "next", "previous"],
                attributes: ["playing"]
            )
        )
        let profile = ScriptingDefinitionParser.profile(from: summary)

        XCTAssertNil(profile.playPauseCommand)
        XCTAssertTrue(profile.supportsPlayPause)
        XCTAssertTrue(profile.requiresStateForPlayPause)
        XCTAssertTrue(profile.isSupported)
    }

    func testLegacyTerminologyNamesAreUsedWhenKeysDiffer() {
        let summary = ScriptingDefinitionParser.summary(
            fromScriptSuite: scriptSuite(
                commands: ["togglePlayback", "nextItem", "previousItem"],
                attributes: ["playbackState"]
            ),
            terminologyData: terminology(
                commands: [
                    "togglePlayback": "playpause",
                    "nextItem": "next track",
                    "previousItem": "previous track",
                ],
                attributes: ["playbackState": "player state"]
            )
        )
        let profile = ScriptingDefinitionParser.profile(from: summary)

        XCTAssertEqual(profile.playPauseCommand, "playpause")
        XCTAssertEqual(profile.nextTrackCommand, "next track")
        XCTAssertEqual(profile.previousTrackCommand, "previous track")
        XCTAssertEqual(profile.stateQuery, .playerState)
        XCTAssertTrue(profile.isSupported)
    }

    func testLegacyScriptSuiteWithoutTerminologyStillWorks() {
        let summary = ScriptingDefinitionParser.summary(
            fromScriptSuite: scriptSuite(
                commands: ["play", "next", "previous"],
                attributes: ["playing"]
            )
        )
        let profile = ScriptingDefinitionParser.profile(from: summary)

        XCTAssertEqual(profile.playPauseCommand, "play")
        XCTAssertTrue(profile.isSupported)
    }

    func testMalformedScriptSuiteFallsBackToNoCommands() {
        let summary = ScriptingDefinitionParser.summary(
            fromScriptSuite: Data("not a property list".utf8)
        )
        let profile = ScriptingDefinitionParser.profile(from: summary)

        XCTAssertEqual(summary, ScriptingDefinitionSummary())
        XCTAssertFalse(profile.isSupported)
    }
}
