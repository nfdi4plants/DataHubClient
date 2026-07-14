open Helpers

initializeContext()

open BasicTasks
open TestTasks
open PackageTasks
open TestPackageTasks
open DocsSampleTasks
open DocumentationTasks

// Force module init so their BuildTask.create calls register the targets.
// Releases run entirely through CI on version tags (see ci.yml); there are no
// interactive release targets.
let _testPackages = testPackages
let _runDocsSamples = runDocsSamples
let _buildDocs = buildDocs
let _watchDocs = watchDocs
let _watchApiDocs = watchApiDocs

[<EntryPoint>]
let main args =
    runOrDefault buildSolution args
