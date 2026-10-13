# --8<-- [start:setup]
import asyncio
from datahub_client import DataHubClient, Authentication
from datahub_client.py.Models.validation_package_ref import ValidationPackageRef

client = DataHubClient("https://git.nfdi4plants.org", Authentication.PersonalAccessToken("your-datahub-pat"))
project_id = 42
# --8<-- [end:setup]

# Offline verification harness, omitted from the documentation snippets.
import json
from collections import deque
from unittest.mock import patch
import httpx

responses = deque([
    [
        "GET",
        "[{\"name\":\"main\",\"type\":\"tree\"},{\"name\":\"README.md\",\"type\":\"blob\"}]"
    ],
    [
        "GET",
        "[{\"name\":\"invenio@3.1.0\",\"type\":\"tree\"}]"
    ],
    [
        "GET",
        "[{\"id\":\"abc123def456\",\"short_id\":\"abc123de\",\"title\":\"Update validation results\",\"message\":\"Update validation results\",\"author_name\":\"ARC Researcher\",\"author_email\":\"researcher@example.org\",\"created_at\":\"2026-10-13T10:00:00Z\"}]"
    ],
    [
        "GET",
        "{\"Critical\":{\"HasFailures\":false,\"Total\":10,\"Passed\":10,\"Failed\":0,\"Errored\":0},\"NonCritical\":{\"HasFailures\":true,\"Total\":4,\"Passed\":3,\"Failed\":1,\"Errored\":0},\"ValidationPackage\":{\"Name\":\"invenio\",\"Version\":\"3.1.0\",\"Summary\":\"Checks ARC publication requirements.\"}}"
    ],
    [
        "GET",
        "<testsuites><testsuite name=\"invenio\" tests=\"10\" failures=\"0\"/></testsuites>"
    ],
    [
        "GET",
        "<svg xmlns=\"http://www.w3.org/2000/svg\"><text>passed</text></svg>"
    ],
    [
        "GET",
        "[{\"name\":\"invenio@3.1.0\",\"type\":\"tree\"}]"
    ],
    [
        "GET",
        "{\"Critical\":{\"HasFailures\":false,\"Total\":10,\"Passed\":10,\"Failed\":0,\"Errored\":0},\"NonCritical\":{\"HasFailures\":true,\"Total\":4,\"Passed\":3,\"Failed\":1,\"Errored\":0},\"ValidationPackage\":{\"Name\":\"invenio\",\"Version\":\"3.1.0\",\"Summary\":\"Checks ARC publication requirements.\"}}"
    ]
])


class OfflineClient:
    async def request(self, method, request_url, headers=None, content=None):
        assert responses, "Unexpected extra HTTP request."
        expected_method, body = responses.popleft()
        assert method == expected_method
        return httpx.Response(200, text=body)

    async def aclose(self):
        pass


mock_client = patch("httpx.AsyncClient", OfflineClient)
mock_client.start()

# --8<-- [start:branches]
async def branches_example():
    return await client.Validation.ListValidatedBranchesAsync(project_id)


branches = asyncio.run(branches_example())
for branch in branches:
    print(branch)
# --8<-- [end:branches]

# --8<-- [start:packages]
async def packages_example():
    return await client.Validation.ListPackagesAsync(project_id, branch="main")


packages = asyncio.run(packages_example())
for package in packages:
    print(package.Path)
# --8<-- [end:packages]

# --8<-- [start:history]
async def history_example():
    return await client.Validation.ListHistoryAsync(project_id, branch="main")


history = asyncio.run(history_example())
for commit in history:
    print(f"{commit.Id}: {commit.Title}")
# --8<-- [end:history]

# --8<-- [start:summary]
package_ref = ValidationPackageRef.Create("invenio", "3.1.0", branch="main")


async def summary_example():
    return await client.Validation.GetSummaryAsync(project_id, package_ref)


summary = asyncio.run(summary_example())
print(f"Critical: {summary.Critical.Passed}/{summary.Critical.Total} passed")
# --8<-- [end:summary]

# --8<-- [start:report]
async def report_example():
    return await client.Validation.GetReportAsync(project_id, package_ref)


report = asyncio.run(report_example())
print(report)
# --8<-- [end:report]

# --8<-- [start:badge]
async def badge_example():
    return await client.Validation.GetBadgeAsync(project_id, package_ref)


badge = asyncio.run(badge_example())
print(badge)
# --8<-- [end:badge]

# --8<-- [start:all-summaries]
async def all_summaries_example():
    return await client.Validation.GetAllSummariesAsync(project_id, branch="main")


summaries = asyncio.run(all_summaries_example())
for item in summaries:
    print(f"{item.ValidationPackage.Name}: {item.Critical.HasFailures}")
# --8<-- [end:all-summaries]

mock_client.stop()
assert not responses, "Expected every sample request to run."
assert list(branches) == ["main"]
assert summary.Critical.Passed == 10
assert len(summaries) == 1
assert "<testsuites>" in report and "<svg" in badge
