module ProjectInfo

open Fake.Core


let project = "DataHubClient"

let testProjects =
    [
        "tests/DataHubClient.Tests/DataHubClient.Tests.fsproj"
        "tests/DataHubClient.DotNet.Tests/DataHubClient.DotNet.Tests.fsproj"
    ]

let sourceProjects =
    [
        "src/DataHubClient.Core/DataHubClient.Core.fsproj"
        "src/DataHubClient.DotNet/DataHubClient.DotNet.fsproj"
    ]

let buildProjects = sourceProjects @ testProjects

let solutionFile  = $"{project}.slnx"

let configuration = "Release"

let gitOwner = "nfdi4plants"

let gitHome = $"https://github.com/{gitOwner}"

let projectRepo = $"https://github.com/{gitOwner}/{project}"

let pkgDir = "pkg"


// Create RELEASE_NOTES.md if not existing. Or "release" would throw an error.
Fake.Extensions.Release.ReleaseNotes.ensure()

let release = ReleaseNotes.load "RELEASE_NOTES.md"

let stableVersion = SemVer.parse release.NugetVersion

let stableVersionTag = (sprintf "%i.%i.%i" stableVersion.Major stableVersion.Minor stableVersion.Patch )

let mutable prereleaseSuffix = ""

let mutable prereleaseTag = ""

let mutable isPrerelease = false

