import Foundation

/// Parses the `data1` payload of NSSystemDefined media key events.
///
/// Layout of `data1` for the auxiliary control button events used by the
/// media keys (NX_KEYTYPE_*):
/// - bits 16...31: key code (16 = play/pause, 17 = next, 18 = previous)
/// - bits 8...15: key state (0x0A = down, 0x0B = up)
enum MediaKeyEventParser {
    static let playPauseKeyCode = 16
    static let nextTrackKeyCode = 17
    static let previousTrackKeyCode = 18
    static let keyDownState = 0x0A

    static func keyCode(fromData1 data1: Int) -> Int {
        (data1 >> 16) & 0xFFFF
    }

    static func isKeyDown(data1: Int) -> Bool {
        ((data1 >> 8) & 0xFF) == keyDownState
    }

    static func action(forKeyCode keyCode: Int) -> MediaKeyAction? {
        switch keyCode {
        case playPauseKeyCode:
            return .playPause
        case nextTrackKeyCode:
            return .nextTrack
        case previousTrackKeyCode:
            return .previousTrack
        default:
            return nil
        }
    }

    static func action(fromData1 data1: Int, requireKeyDown: Bool = true) -> MediaKeyAction? {
        guard let action = action(forKeyCode: keyCode(fromData1: data1)) else {
            return nil
        }
        if requireKeyDown && !isKeyDown(data1: data1) {
            return nil
        }
        return action
    }
}
