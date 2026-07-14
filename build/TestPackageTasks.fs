module TestPackageTasks

open Helpers
open ProjectInfo
open PackageTasks

open BlackFox.Fake
open Fake.Core

open System.IO

/// Each language smoke installs the freshly-packed artifact from `pkg/` like a
/// real end user would, then constructs `DataHubClient` to prove the package
/// metadata, entry point, and transitive deps all resolve. The scripts are
/// regenerated on every run beneath this directory so they can never drift
/// from the F# source.
let private testScriptsDir = Path.Combine(pkgDir, "testScripts")

// --- .NET smoke ----------------------------------------------------------

let private dotnetFsxTemplate =
    """#i "nuget: __SOURCE__"
#r "nuget: DataHubClient, __VERSION__"

open System
open DataHubClient

let url = Environment.GetEnvironmentVariable("DATAHUB_TEST_URL")
let token = Environment.GetEnvironmentVariable("DATAHUB_TEST_TOKEN")
let auth = Authentication.PersonalAccessToken(token)
let client = DataHubClient(url, auth)
let projects = client.Projects.ListAsync() |> Async.RunSynchronously
printfn "dotnet smoke OK: %d projects at %s" projects.Length url
"""

let private dotnetFsxFor (version: string) (localSource: string) =
    dotnetFsxTemplate.Replace("__SOURCE__", localSource).Replace("__VERSION__", version)

let testPackagesDotNet =
    BuildTask.create "TestPackagesDotNet" [ pack ] {
        let version = packageVersion ()
        let dir = Path.Combine(testScriptsDir, "dotnet")
        recreateDir dir
        writeFile (Path.Combine(dir, "smoke.fsx")) (dotnetFsxFor version (Path.GetFullPath pkgDir))
        wipeCachedNuGetExtract "DataHubClient" version
        runCommand "dotnet" [ "fsi"; "smoke.fsx" ] dir
    }

// --- JavaScript smoke ----------------------------------------------------

let private jsPackageJson =
    """{
  "name": "datahub-client-smoke",
  "version": "0.0.0",
  "private": true,
  "type": "module"
}
"""

let private jsSmoke =
    """import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";

const url = process.env.DATAHUB_TEST_URL;
const token = process.env.DATAHUB_TEST_TOKEN;
const auth = Authentication.PersonalAccessToken(token);
const client = new DataHubClient(url, auth);
const projects = await client.Projects.ListAsync();
console.log(`js smoke OK: ${projects.length} projects at ${url}`);
"""

let testPackagesJavaScript =
    BuildTask.create "TestPackagesJavaScript" [ pack ] {
        let version = packageVersion ()
        let dir = Path.Combine(testScriptsDir, "js")
        recreateDir dir
        writeFile (Path.Combine(dir, "package.json")) jsPackageJson
        writeFile (Path.Combine(dir, "smoke.mjs")) jsSmoke
        let tarball =
            Path.GetFullPath(Path.Combine(pkgDir, $"nfdi4plants-datahub-client-{version}.tgz"))
        runCommand "npm" (npmArgs [ "install"; tarball; "--no-audit"; "--no-fund" ]) dir
        runCommand "node" [ "smoke.mjs" ] dir
    }

// --- Python smoke --------------------------------------------------------

let private pySmoke =
    """import asyncio
import os

from datahub_client import DataHubClient, Authentication


async def main() -> None:
    url = os.environ["DATAHUB_TEST_URL"]
    token = os.environ["DATAHUB_TEST_TOKEN"]
    auth = Authentication.PersonalAccessToken(token)
    client = DataHubClient(url, auth)
    projects = await client.Projects.ListAsync()
    print(f"python smoke OK: {len(projects)} projects at {url}")


asyncio.run(main())
"""

let testPackagesPython =
    BuildTask.create "TestPackagesPython" [ pack ] {
        let version = packageVersion ()
        let dir = Path.GetFullPath(Path.Combine(testScriptsDir, "py"))
        recreateDir dir
        writeFile (Path.Combine(dir, "smoke.py")) pySmoke
        // The venv lives outside the repo because `uv venv` places a `lib64`
        // symlink inside it, and the global `Clean` target uses
        // Shell.cleanDirs over `pkg` and `dist`, which throws on symlinks.
        let venv = "/tmp/datahubclient-py-smoke-venv"
        let wheel =
            Path.GetFullPath(Path.Combine(pkgDir, $"datahub_client-{version}-py3-none-any.whl"))
        recreateDir venv
        runCommand "uv" (uvArgs [ "venv"; venv ]) "."
        runCommand "uv" (uvArgs [ "pip"; "install"; "--python"; venv; wheel ]) "."
        // Use the venv's python directly rather than `uv run`, which walks up
        // looking for a pyproject.toml and silently switches to the dev project
        // env, masking the real error from the consumer view.
        runCommand (Path.Combine(venv, "bin", "python")) [ "smoke.py" ] dir
    }

// --- Roll-up -------------------------------------------------------------

let testPackages =
    BuildTask.createEmpty
        "TestPackages"
        [ testPackagesDotNet; testPackagesJavaScript; testPackagesPython ]
