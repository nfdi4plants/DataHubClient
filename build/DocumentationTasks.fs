module DocumentationTasks

open Helpers
open ProjectInfo
open BasicTasks

open BlackFox.Fake

open System.IO

/// The docs site is two generators sharing one output tree: MkDocs Material
/// renders the guide (tabbed, CI-verified polyglot samples — see
/// plans/docs.md) into `site/`, then fsdocs renders the F# API reference into
/// `site/fsdocs/`. Order matters: `mkdocs build` cleans `site/`.
let buildDocs = BuildTask.create "BuildDocs" [buildSolution] {
    printfn "building docs with version %s" stableVersionTag
    runCommand "uv" [ "run"; "--group"; "docs"; "mkdocs"; "build"; "--strict" ] "."
    runDotNet
        (sprintf
            "fsdocs build --eval --clean --input docs/fsdocs --output site/fsdocs --projects %s --properties Configuration=%s --parameters fsdocs-package-version %s"
            (Path.GetFullPath dotNetSourceProject)
            configuration
            stableVersionTag)
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
            "fsdocs watch --eval --input docs/fsdocs --projects %s --properties Configuration=%s --parameters fsdocs-package-version %s"
            (Path.GetFullPath dotNetSourceProject)
            configuration
            stableVersionTag)
        "./"
}
