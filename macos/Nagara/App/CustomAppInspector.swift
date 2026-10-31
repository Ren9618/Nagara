import AppKit
import Foundation

/// Why an app could not be added as a custom media-key target.
enum CustomAppInspectionError: Error, Equatable {
    case invalidApplication
    case notScriptable
    case unsupported(Set<PlayerCommandRequirement>)
    case duplicate
    case preset(PlaybackTarget)
}

/// Validates an app bundle by reading its AppleScript dictionary (`.sdef` or
/// the legacy `.scriptSuite` / `.scriptTerminology` pair). This never launches
/// the app and never sends Apple Events.
enum CustomAppInspector {
    private struct ScriptingResources {
        enum Kind {
            case sdef
            case scriptSuite
        }

        let kind: Kind
        let definitionURL: URL
        let terminologyURL: URL?
    }

    static func inspect(
        url: URL,
        existingBundleIdentifiers: Set<String>
    ) -> Result<CustomPlayerApp, CustomAppInspectionError> {
        guard
            url.pathExtension.lowercased() == "app",
            let bundle = Bundle(url: url),
            let bundleIdentifier = bundle.bundleIdentifier,
            bundle.executableURL != nil
        else {
            return .failure(.invalidApplication)
        }

        if let preset = PlaybackTarget.allCases.first(where: {
            $0.bundleIdentifier == bundleIdentifier
        }) {
            return .failure(.preset(preset))
        }

        guard !existingBundleIdentifiers.contains(bundleIdentifier) else {
            return .failure(.duplicate)
        }

        guard
            let resources = scriptingResources(for: bundle, appURL: url),
            let data = try? Data(contentsOf: resources.definitionURL)
        else {
            return .failure(.notScriptable)
        }

        let summary: ScriptingDefinitionSummary
        switch resources.kind {
        case .sdef:
            summary = ScriptingDefinitionParser.summary(from: data)

        case .scriptSuite:
            let terminologyData = resources.terminologyURL.flatMap {
                try? Data(contentsOf: $0)
            }
            summary = ScriptingDefinitionParser.summary(
                fromScriptSuite: data,
                terminologyData: terminologyData
            )
        }

        let profile = ScriptingDefinitionParser.profile(from: summary)
        guard profile.isSupported else {
            return .failure(.unsupported(profile.missingRequirements))
        }

        let app = CustomPlayerApp(
            bundleIdentifier: bundleIdentifier,
            displayName: displayName(for: bundle, appURL: url),
            profile: profile
        )
        return .success(app)
    }

    private static func scriptingResources(for bundle: Bundle, appURL: URL) -> ScriptingResources? {
        let fileManager = FileManager.default
        let resourcesURL = appURL.appendingPathComponent("Contents/Resources", isDirectory: true)

        if let declared = bundle.object(forInfoDictionaryKey: "OSAScriptingDefinition") as? String,
           !declared.isEmpty {
            let candidates = [
                resourcesURL.appendingPathComponent(declared),
                appURL.appendingPathComponent(declared),
            ]
            for candidate in candidates where fileManager.fileExists(atPath: candidate.path) {
                if let resources = resources(for: candidate) {
                    return resources
                }
            }
        }

        guard let contents = try? fileManager.contentsOfDirectory(
            at: resourcesURL,
            includingPropertiesForKeys: [.isDirectoryKey],
            options: [.skipsHiddenFiles]
        ) else {
            return nil
        }

        if let resources = resources(in: contents) {
            return resources
        }

        // Some apps keep the dictionary inside a language folder.
        for directory in contents where directory.pathExtension.lowercased() == "lproj" {
            guard let inner = try? fileManager.contentsOfDirectory(
                at: directory,
                includingPropertiesForKeys: nil,
                options: [.skipsHiddenFiles]
            ) else {
                continue
            }
            if let resources = resources(in: inner) {
                return resources
            }
        }

        return nil
    }

    private static func resources(in directoryContents: [URL]) -> ScriptingResources? {
        if let definition = directoryContents.first(where: {
            $0.pathExtension.lowercased() == "sdef"
        }) {
            return resources(for: definition)
        }

        if let definition = directoryContents.first(where: {
            $0.pathExtension.lowercased() == "scriptsuite"
        }) {
            return resources(for: definition)
        }

        return nil
    }

    private static func resources(for definitionURL: URL) -> ScriptingResources? {
        switch definitionURL.pathExtension.lowercased() {
        case "sdef":
            return ScriptingResources(
                kind: .sdef,
                definitionURL: definitionURL,
                terminologyURL: nil
            )

        case "scriptsuite":
            let expected = definitionURL
                .deletingPathExtension()
                .appendingPathExtension("scriptTerminology")
            if FileManager.default.fileExists(atPath: expected.path) {
                return ScriptingResources(
                    kind: .scriptSuite,
                    definitionURL: definitionURL,
                    terminologyURL: expected
                )
            }

            let directory = definitionURL.deletingLastPathComponent()
            let terminology = (try? FileManager.default.contentsOfDirectory(
                at: directory,
                includingPropertiesForKeys: nil,
                options: [.skipsHiddenFiles]
            ))?.first(where: {
                $0.pathExtension.lowercased() == "scriptterminology"
            })

            return ScriptingResources(
                kind: .scriptSuite,
                definitionURL: definitionURL,
                terminologyURL: terminology
            )

        default:
            return nil
        }
    }

    private static func displayName(for bundle: Bundle, appURL: URL) -> String {
        if let name = bundle.object(forInfoDictionaryKey: "CFBundleDisplayName") as? String,
           !name.isEmpty {
            return name
        }
        if let name = bundle.object(forInfoDictionaryKey: "CFBundleName") as? String,
           !name.isEmpty {
            return name
        }
        return appURL.deletingPathExtension().lastPathComponent
    }
}
