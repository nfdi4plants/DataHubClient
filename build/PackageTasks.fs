module PackageTasks

open ProjectInfo

open BasicTasks
open TestTasks

open BlackFox.Fake
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators

open System.IO
open System.Text.RegularExpressions

/// https://github.com/Freymaurer/Fake.Extensions.Release#release-notes-in-nuget
let private replaceCommitLink input =
    let commitLinkPattern = @"\[\[#[a-z0-9]*\]\(.*\)\] "
    Regex.Replace(input, commitLinkPattern, "")

let private releaseNotesText =
    release.Notes |> List.map replaceCommitLink |> String.concat "\r\n"

let private runCommand command args workingDirectory =
    let result =
        CreateProcess.fromRawCommand command args
        |> CreateProcess.withWorkingDirectory workingDirectory
        |> Proc.run

    if result.ExitCode <> 0 then
        failwithf "%s failed with exit code %i" command result.ExitCode

let private uvArgs args =
    [ "--cache-dir"; "/tmp/datahubclient-uv-cache" ] @ args

let private npmArgs args =
    [ "--cache"; "/tmp/datahubclient-npm-cache" ] @ args

let private replaceFirst (pattern: string) (replacement: string) (input: string) =
    Regex(pattern).Replace(input, replacement, 1)

let private writeVersionedPackageJson version outputDirectory =
    let source = "src/DataHubClient/package.json"
    let target = Path.Combine(outputDirectory, "package.json")

    let text =
        File.ReadAllText(source)
        |> replaceFirst "\"version\"\\s*:\\s*\"[^\"]+\"" $"\"version\": \"{version}\""

    File.WriteAllText(target, text)

/// Stamp the wheel's project version in the root pyproject.toml in place. The
/// build derives `version` from RELEASE_NOTES.md; this is the same regex-based
/// override we use for package.json, just pointed at the single root pyproject
/// that drives the wheel build (no template/copy step — Fable transpiles into
/// `src/DataHubClient/py/` and poetry-core picks the version up from here).
let private setRootPyprojectVersion version =
    let path = "pyproject.toml"

    let text =
        File.ReadAllText(path)
        |> replaceFirst "(?m)^version\\s*=\\s*\"[^\"]+\"" $"version = \"{version}\""

    File.WriteAllText(path, text)

let private removeFableModulesGitIgnore outputDirectory =
    let path = Path.Combine(outputDirectory, "fable_modules", ".gitignore")

    if File.Exists(path) then
        File.Delete(path)

let private packNuGet version versionSuffix =
    dotNetSourceProject
    |> DotNet.pack (fun p ->
        let msBuildParams =
            { p.MSBuildParams with
                Properties =
                    [ "Version", version; "PackageReleaseNotes", releaseNotesText ]
                    @ p.MSBuildParams.Properties
                DisableInternalBinLog = true }

        { p with
            MSBuildParams = msBuildParams
            OutputPath = Some pkgDir
            VersionSuffix = versionSuffix }
        |> DotNet.Options.withCustomParams (Some "-tl"))

let private transpile project lang outputDirectory =
    Shell.cleanDir outputDirectory

    let result =
        DotNet.exec id "fable" $"{project} --lang {lang} -o {outputDirectory} --noCache"

    if not result.OK then
        failwithf "Fable transpilation failed for %s" project

let private packNpm version =
    let outputDirectory = "dist/js"
    transpile javaScriptSourceProject "javascript" outputDirectory
    removeFableModulesGitIgnore outputDirectory
    writeVersionedPackageJson version outputDirectory
    Directory.CreateDirectory(pkgDir) |> ignore
    runCommand "npm" (npmArgs [ "pack"; $"./{outputDirectory}"; "--pack-destination"; pkgDir ]) "."

let private packPython version =
    // Fable lands inside the source tree (sibling of the .fs files), so the
    // hand-written src/DataHubClient/__init__.py survives the transpile and
    // hatchling can pack `src/DataHubClient/` → `datahub_client/` whole. This
    // mirrors ARCtrl's Python build layout.
    let outputDirectory = "src/DataHubClient/py"
    transpile pythonSourceProject "python" outputDirectory
    removeFableModulesGitIgnore outputDirectory
    setRootPyprojectVersion version
    Directory.CreateDirectory(pkgDir) |> ignore
    runCommand "uv" (uvArgs [ "build"; "--wheel"; "--out-dir"; Path.GetFullPath(pkgDir) ]) "."

let private packAll version versionSuffix =
    Directory.CreateDirectory(pkgDir) |> ignore
    packNuGet version versionSuffix
    packNpm version
    packPython version

let pack =
    BuildTask.create "Pack" [ clean; buildSolution; runTestsAll ] { packAll (packageVersion ()) None }

let packPrerelease =
    BuildTask.create "PackPrerelease" [ setPrereleaseTag; clean; buildSolution; runTestsAll ] {
        packAll (packageVersion ()) (Some prereleaseSuffix)
    }
