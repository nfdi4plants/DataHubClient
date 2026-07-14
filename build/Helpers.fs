module Helpers

open BlackFox.Fake
open Fake.Core
open Fake.DotNet

open System.IO

let initializeContext () =
    let execContext = Context.FakeExecutionContext.Create false "build.fsx" [ ]
    Context.setExecutionContext (Context.RuntimeContext.Fake execContext)

/// Executes a dotnet command in the given working directory
let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let runCommand command args workingDirectory =
    let result =
        CreateProcess.fromRawCommand command args
        |> CreateProcess.withWorkingDirectory workingDirectory
        |> Proc.run

    if result.ExitCode <> 0 then
        failwithf "%s failed with exit code %i" command result.ExitCode

let uvArgs args =
    [ "--cache-dir"; "/tmp/datahubclient-uv-cache" ] @ args

let npmArgs args =
    [ "--cache"; "/tmp/datahubclient-npm-cache" ] @ args

let writeFile (path: string) (content: string) =
    Directory.CreateDirectory(Path.GetDirectoryName path) |> ignore
    File.WriteAllText(path, content)

/// Recreate `path` empty. Uses `Directory.Delete(_, recursive = true)` rather
/// than FAKE's `Shell.cleanDir`, which throws on the symlinks `uv venv`
/// creates (e.g. `.venv/lib64`).
let recreateDir (path: string) =
    if Directory.Exists path then Directory.Delete(path, true)
    Directory.CreateDirectory path |> ignore

/// `#r "nuget: <id>"` in FSI resolves from ~/.nuget/packages. During local
/// iteration the version stays at the same number, so a previously-extracted
/// copy of <id>/<version>/ shadows a freshly-packed .nupkg in `pkg/`. Targeted
/// wipe before FSI runs.
let wipeCachedNuGetExtract (packageId: string) (version: string) =
    let cachedExtract =
        Path.Combine(
            System.Environment.GetEnvironmentVariable "HOME",
            ".nuget",
            "packages",
            packageId.ToLowerInvariant(),
            version)
    if Directory.Exists cachedExtract then
        Directory.Delete(cachedExtract, true)

let runOrDefault defaultTarget args =
    Trace.trace (sprintf "%A" args)
    try
        match args with
        | [| target |] -> Target.runOrDefault target
        | arr when args.Length > 1 ->
            Target.run 0 (Array.head arr) ( Array.tail arr |> List.ofArray )
        | _ -> BuildTask.runOrDefault defaultTarget
        0
    with e ->
        printfn "%A" e
        1