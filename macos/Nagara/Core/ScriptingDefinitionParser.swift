import Foundation

/// The command and property names found in an app's AppleScript dictionary.
struct ScriptingDefinitionSummary: Equatable {
    var commandNames: Set<String> = []
    var propertyNames: Set<String> = []
}

/// Reads AppleScript dictionary information without launching the app or
/// sending Apple Events. Detection is therefore safe to run when the user
/// adds an app in Settings.
///
/// Modern apps ship an XML `.sdef`. Older apps (for example VLC) use the
/// legacy `.scriptSuite` / `.scriptTerminology` property-list pair instead.
enum ScriptingDefinitionParser {
    static func summary(from data: Data) -> ScriptingDefinitionSummary {
        let delegate = Delegate()
        let parser = XMLParser(data: data)
        parser.delegate = delegate
        parser.shouldResolveExternalEntities = false
        parser.parse()
        return delegate.summary
    }

    /// Parses the legacy `.scriptSuite` format. Command and attribute keys in
    /// the suite are supplemented by the human-readable names from the
    /// matching `.scriptTerminology` file.
    static func summary(
        fromScriptSuite data: Data,
        terminologyData: Data? = nil
    ) -> ScriptingDefinitionSummary {
        var summary = ScriptingDefinitionSummary()

        if let suite = propertyList(from: data) {
            if let commands = suite["Commands"] as? [String: Any] {
                summary.commandNames.formUnion(
                    commands.keys.map(normalize)
                )
            }
            if let classes = suite["Classes"] as? [String: Any] {
                for case let classInfo as [String: Any] in classes.values {
                    guard let attributes = classInfo["Attributes"] as? [String: Any] else {
                        continue
                    }
                    summary.propertyNames.formUnion(
                        attributes.keys.map(normalize)
                    )
                }
            }
        }

        if let terminologyData, let terminology = propertyList(from: terminologyData) {
            if let commands = terminology["Commands"] as? [String: Any] {
                for (key, value) in commands {
                    if
                        let command = value as? [String: Any],
                        let name = command["Name"] as? String
                    {
                        summary.commandNames.insert(normalize(name))
                    } else {
                        summary.commandNames.insert(normalize(key))
                    }
                }
            }
            if let classes = terminology["Classes"] as? [String: Any] {
                for case let classInfo as [String: Any] in classes.values {
                    guard let attributes = classInfo["Attributes"] as? [String: Any] else {
                        continue
                    }
                    for (key, value) in attributes {
                        if
                            let attribute = value as? [String: Any],
                            let name = attribute["Name"] as? String
                        {
                            summary.propertyNames.insert(normalize(name))
                        } else {
                            summary.propertyNames.insert(normalize(key))
                        }
                    }
                }
            }
        }

        return summary
    }

    static func profile(from summary: ScriptingDefinitionSummary) -> PlayerCommandProfile {
        let commands = summary.commandNames
        let properties = summary.propertyNames

        func firstCommand(_ aliases: [String]) -> String? {
            aliases.first(where: { commands.contains($0) })
        }

        let playPauseCommand = firstCommand([
            "playpause",
            "play pause",
            "play/pause",
            "toggle playpause",
            "toggle play/pause",
        ])
        let playCommand = commands.contains("play") ? "play" : nil
        let pauseCommand = commands.contains("pause") ? "pause" : nil
        let nextTrackCommand = firstCommand([
            "next track",
            "next",
            "skip forward",
            "skip next",
        ])
        let previousTrackCommand = firstCommand([
            "previous track",
            "previous",
            "skip backward",
            "skip previous",
        ])

        // VLC and some legacy apps expose a single `play` command that
        // toggles play/pause. Only treat it as a toggle when there is no
        // separate `pause` command; apps with both use the stateful pair.
        let toggleCommand = playPauseCommand ?? (pauseCommand == nil ? playCommand : nil)

        let stateQuery: PlayerCommandProfile.StateQuery?
        if properties.contains("player state") {
            stateQuery = .playerState
        } else if properties.contains("player status") {
            stateQuery = .playerStatus
        } else if properties.contains("playing") {
            stateQuery = .playing
        } else {
            stateQuery = nil
        }

        return PlayerCommandProfile(
            playPauseCommand: toggleCommand,
            playCommand: playCommand,
            pauseCommand: pauseCommand,
            nextTrackCommand: nextTrackCommand,
            previousTrackCommand: previousTrackCommand,
            stateQuery: stateQuery
        )
    }

    static func normalize(_ name: String) -> String {
        name
            .lowercased()
            .split(whereSeparator: { $0.isWhitespace })
            .joined(separator: " ")
    }

    private static func propertyList(from data: Data) -> [String: Any]? {
        (try? PropertyListSerialization.propertyList(
            from: data,
            options: [],
            format: nil
        )) as? [String: Any]
    }

    private final class Delegate: NSObject, XMLParserDelegate {
        var summary = ScriptingDefinitionSummary()

        func parser(
            _ parser: XMLParser,
            didStartElement elementName: String,
            namespaceURI: String?,
            qualifiedName qName: String?,
            attributes attributeDict: [String: String] = [:]
        ) {
            guard let name = attributeDict["name"] else {
                return
            }
            let normalized = ScriptingDefinitionParser.normalize(name)
            switch elementName {
            case "command":
                summary.commandNames.insert(normalized)
            case "property":
                summary.propertyNames.insert(normalized)
            default:
                break
            }
        }
    }
}
