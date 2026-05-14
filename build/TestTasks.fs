module TestTasks

open BlackFox.Fake
open Fake.DotNet

open ProjectInfo
open BasicTasks


let runTests = BuildTask.create "RunTests" [clean; buildSolution] {
    testProjects
    |> Seq.iter (fun testProject ->
        let result =
            DotNet.exec
                (fun opts ->
                    { opts with
                        CustomParams = Some "-tl"
                    })
                "run"
                $"--project {testProject} --configuration {configuration} --no-build"
        if not result.OK then
            failwithf "Tests failed for %s" testProject
    )
}
