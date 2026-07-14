module DocsSampleTasks

open Helpers
open ProjectInfo
open PackageTasks

open BlackFox.Fake

open System.IO

/// Every doc sample under `docs/samples/<topic>/` is a real program per
/// language, executed here against the freshly-packed artifacts from `pkg/`
/// exactly the way a real end user consumes them (see TestPackageTasks for the
/// same consumer pattern). The doc pages include these files verbatim via
/// pymdownx snippets, so a sample that renders on the site is by construction
/// a sample that ran green in CI — for all three languages, not just F#.
/// Samples must not hit the network: PR CI has no live DataHub.
let private samplesSourceDir = Path.Combine("docs", "samples")

let private samplesScratchDir = Path.Combine(pkgDir, "docsSamples")

let private samplesByExtension (extension: string) =
    if Directory.Exists samplesSourceDir then
        Directory.GetFiles(samplesSourceDir, "sample" + extension, SearchOption.AllDirectories)
        |> Array.sort
    else
        [||]

let private topicOf (samplePath: string) =
    Path.GetFileName(Path.GetDirectoryName samplePath)

// --- F# -------------------------------------------------------------------

/// The committed .fsx opens with the reference line a script user writes; the
/// runner swaps it for a local-source pin so the sample resolves the
/// freshly-packed .nupkg instead of nuget.org.
let private fsxReferenceLine = "#r \"nuget: DataHubClient\""

let private pinnedFsx (version: string) (samplePath: string) =
    let pin =
        $"#i \"nuget: {Path.GetFullPath pkgDir}\"\n#r \"nuget: DataHubClient, {version}\""

    let source = File.ReadAllText samplePath

    if not (source.Contains fsxReferenceLine) then
        failwithf "%s must reference the package with the line %s" samplePath fsxReferenceLine

    source.Replace(fsxReferenceLine, pin)

let runDocsSamplesDotNet =
    BuildTask.create "RunDocsSamplesDotNet" [ pack ] {
        let version = packageVersion ()
        let dir = Path.Combine(samplesScratchDir, "dotnet")
        recreateDir dir
        wipeCachedNuGetExtract "DataHubClient" version

        for sample in samplesByExtension ".fsx" do
            let script = topicOf sample + ".fsx"
            writeFile (Path.Combine(dir, script)) (pinnedFsx version sample)
            runCommand "dotnet" [ "fsi"; script ] dir
    }

// --- JavaScript -------------------------------------------------------------

let private jsPackageJson =
    """{
  "name": "datahub-client-docs-samples",
  "version": "0.0.0",
  "private": true,
  "type": "module"
}
"""

/// The committed .mjs files run unmodified; they only need to sit next to a
/// node_modules holding the packed tarball so the package import resolves.
let runDocsSamplesJavaScript =
    BuildTask.create "RunDocsSamplesJavaScript" [ pack ] {
        let version = packageVersion ()
        let dir = Path.Combine(samplesScratchDir, "js")
        recreateDir dir
        writeFile (Path.Combine(dir, "package.json")) jsPackageJson
        let tarball =
            Path.GetFullPath(Path.Combine(pkgDir, $"nfdi4plants-datahub-client-{version}.tgz"))
        runCommand "npm" (npmArgs [ "install"; tarball; "--no-audit"; "--no-fund" ]) dir

        for sample in samplesByExtension ".mjs" do
            let script = topicOf sample + ".mjs"
            File.Copy(sample, Path.Combine(dir, script), true)
            runCommand "node" [ script ] dir
    }

// --- Python -----------------------------------------------------------------

/// The committed .py files run unmodified against a venv holding the packed
/// wheel. The venv lives outside the repo for the same `lib64` symlink reason
/// as the smoke-test venv (see TestPackageTasks).
let runDocsSamplesPython =
    BuildTask.create "RunDocsSamplesPython" [ pack ] {
        let version = packageVersion ()
        let venv = "/tmp/datahubclient-py-docs-venv"
        let wheel =
            Path.GetFullPath(Path.Combine(pkgDir, $"datahub_client-{version}-py3-none-any.whl"))
        recreateDir venv
        runCommand "uv" (uvArgs [ "venv"; venv ]) "."
        runCommand "uv" (uvArgs [ "pip"; "install"; "--python"; venv; wheel ]) "."

        for sample in samplesByExtension ".py" do
            // The venv's python directly rather than `uv run`, which walks up to
            // the dev project's pyproject.toml and switches environments.
            runCommand (Path.Combine(venv, "bin", "python")) [ Path.GetFileName sample ] (Path.GetDirectoryName sample)
    }

// --- Roll-up -----------------------------------------------------------------

let runDocsSamples =
    BuildTask.createEmpty
        "RunDocsSamples"
        [ runDocsSamplesDotNet; runDocsSamplesJavaScript; runDocsSamplesPython ]
