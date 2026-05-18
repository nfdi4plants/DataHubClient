module BasicTasks

open BlackFox.Fake
open Fake.IO
open Fake.DotNet
open Fake.IO.Globbing.Operators

open System.IO

open ProjectInfo

let clean = BuildTask.create "Clean" [] {
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ "tests/**/bin"
    ++ "tests/**/obj"
    ++ "pkg"
    ++ "dist"
    |> Shell.cleanDirs
}

let private escapedString (value: string) =
    value.Replace("\\", "\\\\").Replace("\"", "\\\"")

let private versionSource (version: string) =
    let escapedVersion = escapedString version

    String.concat "\n" [
        "namespace DataHubClient"
        ""
        "open Fable.Core"
        ""
        "/// <summary>"
        "/// Version information for the DataHubClient library, shared by the .NET,"
        "/// JavaScript/TypeScript, and Python packages."
        "/// </summary>"
        "[<AttachMembers>]"
        "type DataHubClientVersion ="
        ""
        "    /// <summary>The package version compiled into this DataHubClient build.</summary>"
        $"    static member Value = \"{escapedVersion}\""
        ""
        "    /// <summary>A user-agent compatible product token for this DataHubClient build.</summary>"
        $"    static member UserAgent = \"DataHubClient/{escapedVersion}\""
        ""
        "    /// <summary>The request header used to report the DataHubClient version to a DataHub.</summary>"
        "    static member HeaderName = \"X-DataHubClient-Version\""
        ""
    ]

let generateVersionSource = BuildTask.create "GenerateVersionSource" [ clean ] {
    let version = packageVersion ()
    Directory.CreateDirectory(Path.GetDirectoryName(versionSourceFile)) |> ignore
    File.WriteAllText(versionSourceFile, versionSource version)
}

let setPrereleaseTag = BuildTask.create "SetPrereleaseTag" [] {
    printfn "Please enter pre-release package suffix"
    let suffix = System.Console.ReadLine()
    prereleaseSuffix <- suffix
    prereleaseTag <- (sprintf "%s-%s" release.NugetVersion suffix)
    isPrerelease <- true
}

/// Builds all source and test projects. The repository still keeps the .slnx
/// manifest, but .NET 10.0.300 currently fails the solution restore target
/// without diagnostics in this container.
let buildSolution =
    BuildTask.create "BuildSolution" [ generateVersionSource ] {
        buildProjects
        |> Seq.iter (fun project ->
            project
            |> DotNet.build (fun p ->
                { p with MSBuildParams = { p.MSBuildParams with DisableInternalBinLog = true }}
                |> DotNet.Options.withCustomParams (Some "-tl")
            )
        )
    }

