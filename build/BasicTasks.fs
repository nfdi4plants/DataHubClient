module BasicTasks

open BlackFox.Fake
open Fake.IO
open Fake.DotNet
open Fake.IO.Globbing.Operators

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
    BuildTask.create "BuildSolution" [ clean ] {
        buildProjects
        |> Seq.iter (fun project ->
            project
            |> DotNet.build (fun p ->
                { p with MSBuildParams = { p.MSBuildParams with DisableInternalBinLog = true }}
                |> DotNet.Options.withCustomParams (Some "-tl")
            )
        )
    }

