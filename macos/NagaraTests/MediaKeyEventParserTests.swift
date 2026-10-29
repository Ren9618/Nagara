import XCTest

final class MediaKeyEventParserTests: XCTestCase {
    private func makeData1(keyCode: Int, state: Int, flags: Int = 0) -> Int {
        (keyCode << 16) | (state << 8) | flags
    }

    func testKeyCodeIsReadFromHighBits() {
        XCTAssertEqual(MediaKeyEventParser.keyCode(fromData1: makeData1(keyCode: 16, state: 0x0A)), 16)
        XCTAssertEqual(MediaKeyEventParser.keyCode(fromData1: makeData1(keyCode: 17, state: 0x0A)), 17)
        XCTAssertEqual(MediaKeyEventParser.keyCode(fromData1: makeData1(keyCode: 18, state: 0x0A)), 18)
    }

    func testKeyDownAndKeyUpStates() {
        XCTAssertTrue(MediaKeyEventParser.isKeyDown(data1: makeData1(keyCode: 16, state: 0x0A)))
        XCTAssertFalse(MediaKeyEventParser.isKeyDown(data1: makeData1(keyCode: 16, state: 0x0B)))
    }

    func testMapsSupportedKeyCodesToActions() {
        XCTAssertEqual(MediaKeyEventParser.action(forKeyCode: 16), .playPause)
        XCTAssertEqual(MediaKeyEventParser.action(forKeyCode: 17), .nextTrack)
        XCTAssertEqual(MediaKeyEventParser.action(forKeyCode: 18), .previousTrack)
    }

    func testIgnoresUnsupportedKeyCodes() {
        XCTAssertNil(MediaKeyEventParser.action(forKeyCode: 0))
        XCTAssertNil(MediaKeyEventParser.action(forKeyCode: 19))
    }

    func testActionRequiresKeyDownByDefault() {
        XCTAssertEqual(
            MediaKeyEventParser.action(fromData1: makeData1(keyCode: 16, state: 0x0A)),
            .playPause
        )
        XCTAssertNil(
            MediaKeyEventParser.action(fromData1: makeData1(keyCode: 16, state: 0x0B))
        )
    }

    func testActionCanBeReadFromKeyUpWhenRequested() {
        XCTAssertEqual(
            MediaKeyEventParser.action(fromData1: makeData1(keyCode: 17, state: 0x0B), requireKeyDown: false),
            .nextTrack
        )
    }

    func testFlagsInLowBitsDoNotAffectParsing() {
        let data1 = makeData1(keyCode: 18, state: 0x0A, flags: 0x00FF)
        XCTAssertEqual(MediaKeyEventParser.action(fromData1: data1), .previousTrack)
    }
}
