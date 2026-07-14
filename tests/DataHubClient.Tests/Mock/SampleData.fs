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

let validationBranchTree =
    """[{"id":"t1","name":"main","type":"tree","path":"main","mode":"040000","ignored":"ok"},{"id":"t2","name":"dev","type":"tree","path":"dev","mode":"040000","ignored":"ok"},{"id":"t3","name":"README.md","type":"blob","path":"README.md","mode":"100644","ignored":"ok"}]"""

let validationPackageTree =
    """[{"id":"t4","name":"invenio@3.1.0","type":"tree","path":"main/invenio@3.1.0","mode":"040000","ignored":"ok"},{"id":"t5","name":"pride@1.0.2","type":"tree","path":"main/pride@1.0.2","mode":"040000","ignored":"ok"},{"id":"t6","name":"unversioned","type":"tree","path":"main/unversioned","mode":"040000","ignored":"ok"},{"id":"t7","name":"badge.svg","type":"blob","path":"main/badge.svg","mode":"100644","ignored":"ok"}]"""

let validationSummary =
    """{"Critical":{"HasFailures":false,"Total":10,"Passed":10,"Failed":0,"Errored":0,"ignored":"ok"},"NonCritical":{"HasFailures":true,"Total":4,"Passed":2,"Failed":1,"Errored":1},"ValidationPackage":{"Name":"invenio","Version":"3.1.0","Summary":"Validates invenio publishability","Description":"Checks the ARC against invenio requirements","CQCHookEndpoint":"https://hub.example/hooks/invenio","ignored":"ok"},"ignored":"ok"}"""

let prideValidationSummary =
    validationSummary.Replace("\"Name\":\"invenio\"", "\"Name\":\"pride\"")

let validationReport =
    """<?xml version="1.0" encoding="UTF-8"?><testsuites><testsuite name="invenio" tests="10"/></testsuites>"""

let validationBadge =
    """<svg xmlns="http://www.w3.org/2000/svg"><text>passed</text></svg>"""
