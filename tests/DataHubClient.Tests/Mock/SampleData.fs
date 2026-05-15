module DataHubClient.Tests.Mock.SampleData

let user =
    """{"id":7,"username":"carl","name":"Carl Correns","state":"active","web_url":"https://hub.example/carl","avatar_url":"https://hub.example/avatar.png","ignored":"ok"}"""

let project =
    """{"id":42,"name":"My ARC","path":"my-arc","path_with_namespace":"lab/my-arc","visibility":"private","web_url":"https://hub.example/lab/my-arc","description":"An annotated research context","default_branch":"main","ignored":"ok"}"""

let projects =
    "[" + project + "]"

let commit =
    """{"id":"abc123def456","short_id":"abc123de","title":"Add ARC metadata","message":"Add ARC metadata\n\nlong body","author_name":"Carl Correns","author_email":"carl@hub.example","created_at":"2026-05-01T10:00:00Z","web_url":"https://hub.example/lab/my-arc/-/commit/abc123de","ignored":"ok"}"""

let commits =
    "[" + commit + "]"

let branch =
    """{"name":"main","default":true,"protected":true,"merged":false,"commit":""" + commit + ""","ignored":"ok"}"""

let branches =
    "[" + branch + "]"

let repoFile =
    """{"file_name":"isa.assay.xlsx","file_path":"assays/a/isa.assay.xlsx","size":1024,"encoding":"base64","content":"QVJD","ref":"main","blob_id":"blob1","commit_id":"commit1","ignored":"ok"}"""

let issue =
    """{"id":100,"iid":3,"project_id":42,"title":"Missing assay metadata","state":"opened","author":""" + user + ""","assignees":[""" + user + """],"labels":["bug","metadata"],"web_url":"https://hub.example/lab/my-arc/-/issues/3","created_at":"2026-05-01T00:00:00Z","updated_at":"2026-05-03T00:00:00Z","description":"please fix","ignored":"ok"}"""

let closedIssue =
    issue.Replace("\"state\":\"opened\"", "\"state\":\"closed\"")

let issues =
    "[" + issue + "]"

let mergeRequest =
    """{"id":200,"iid":8,"project_id":42,"title":"Add assay","state":"opened","source_branch":"feature/assay","target_branch":"main","author":""" + user + ""","web_url":"https://hub.example/lab/my-arc/-/merge_requests/8","created_at":"2026-05-01T00:00:00Z","updated_at":"2026-05-04T00:00:00Z","description":"adds an assay","merge_status":"can_be_merged","ignored":"ok"}"""

let mergeRequests =
    "[" + mergeRequest + "]"

let note =
    """{"id":5,"body":"Looks good","author":""" + user + ""","system":false,"created_at":"2026-05-02T09:00:00Z","updated_at":"2026-05-02T09:00:00Z","ignored":"ok"}"""

let notes =
    "[" + note + "]"

let package =
    """{"id":9,"name":"arc-bundle","version":"1.2.0","package_type":"generic","status":"default","created_at":"2026-05-01T00:00:00Z","web_url":"https://hub.example/lab/my-arc/-/packages/9","ignored":"ok"}"""

let packages =
    "[" + package + "]"
