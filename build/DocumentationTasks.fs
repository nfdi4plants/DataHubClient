module DocumentationTasks

open Helpers
open ProjectInfo
open BasicTasks

open BlackFox.Fake

open System.IO

/// Where the generated API reference is served from, used for the fsdocs `root`
/// substitution. fsdocs defaults `root` to `<PackageProjectUrl>`, which points at the
/// GitHub repo — every stylesheet and cross-page link would then resolve under
/// github.com and 404. MkDocs owns https://nfdi4plants.github.io/DataHubClient/, so the
/// reference lives one level below it, and `<PackageProjectUrl>` cannot simply be
/// changed to this value without mislabelling the package's project URL.
let private fsdocsRoot = "/DataHubClient/fsdocs/"

/// The docs site is two generators sharing one output tree: MkDocs Material
/// renders the guide (tabbed, CI-verified polyglot samples — see
/// plans/docs.md) into `site/`, then fsdocs renders the F# API reference into
/// `site/fsdocs/`. Order matters: `mkdocs build` cleans `site/`.
let buildDocs = BuildTask.create "BuildDocs" [buildSolution] {
    printfn "building docs with version %s" stableVersionTag
    // Read by the `!ENV` tag on `extra.version` in mkdocs.yml and rendered into the
    // announcement bar by overrides/main.html. Set on this process rather than passed
    // as an argument because mkdocs has no CLI flag for arbitrary config values; the
    // child process inherits it. Unset for a bare `mkdocs serve`, which then says "dev".
    System.Environment.SetEnvironmentVariable("DATAHUBCLIENT_DOCS_VERSION", stableVersionTag)
    runCommand "uv" [ "run"; "--group"; "docs"; "mkdocs"; "build"; "--strict" ] "."
    runDotNet
        (sprintf
            "fsdocs build --eval --clean --input docs/fsdocs --output site/fsdocs --projects %s --properties Configuration=%s --parameters fsdocs-package-version %s root %s"
            (Path.GetFullPath dotNetSourceProject)
            configuration
            stableVersionTag
            fsdocsRoot)
        "./"
}

/// Live-reload preview of the guide (mkdocs serve, http://127.0.0.1:8000).
/// Edits to pages, mkdocs.yml, and the included docs/samples/ files all
/// hot-reload. The fsdocs reference is not part of this loop — use
/// WatchApiDocs for that, or BuildDocs for the full site.
let watchDocs = BuildTask.create "WatchDocs" [] {
    runCommand "uv" [ "run"; "--group"; "docs"; "mkdocs"; "serve" ] "."
}

/// Live-reload preview of the fsdocs F# API reference (fsdocs watch,
/// http://127.0.0.1:8901) — rebuilds on XML doc comment changes.
let watchApiDocs = BuildTask.create "WatchApiDocs" [buildSolution] {
    runDotNet
        (sprintf
            // root is "/" here, not fsdocsRoot: the watch server serves the reference at
            // the origin root, so the deployed /DataHubClient/fsdocs/ prefix would break
            // every local stylesheet and link.
            "fsdocs watch --eval --input docs/fsdocs --projects %s --properties Configuration=%s --parameters fsdocs-package-version %s root /"
            (Path.GetFullPath dotNetSourceProject)
            configuration
            stableVersionTag)
        "./"
}
