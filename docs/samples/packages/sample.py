# --8<-- [start:setup]
import asyncio
from datahub_client import DataHubClient, Authentication

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
        "[{\"id\":9,\"name\":\"arc-export\",\"version\":\"1.0.0\",\"package_type\":\"generic\",\"status\":\"default\",\"created_at\":\"2026-10-13T10:00:00Z\"}]"
    ],
    [
        "GET",
        "{\"id\":9,\"name\":\"arc-export\",\"version\":\"1.0.0\",\"package_type\":\"generic\",\"status\":\"default\",\"created_at\":\"2026-10-13T10:00:00Z\"}"
    ],
    [
        "PUT",
        "{\"message\":\"201 Created\"}"
    ],
    [
        "GET",
        "{\"name\":\"My ARC\"}"
    ]
])


class OfflineClient:
    def __init__(self, *, follow_redirects, **kwargs):
        assert follow_redirects is True

    async def request(self, method, request_url, headers=None, content=None):
        assert responses, "Unexpected extra HTTP request."
        expected_method, body = responses.popleft()
        assert method == expected_method
        return httpx.Response(200, text=body)

    async def aclose(self):
        pass


mock_client = patch("httpx.AsyncClient", OfflineClient)
mock_client.start()

# --8<-- [start:list]
async def list_example():
    return await client.Packages.ListAsync(
        project_id, package_type="generic", package_name="arc-export"
    )


packages = asyncio.run(list_example())
for package in packages:
    print(f"{package.Name}@{package.Version}")
# --8<-- [end:list]

# --8<-- [start:get]
async def get_example():
    return await client.Packages.GetAsync(project_id, 9)


package = asyncio.run(get_example())
print(package.Version)
# --8<-- [end:get]

# --8<-- [start:upload]
async def upload_example():
    return await client.Packages.UploadGenericFileAsync(
        project_id, "arc-export", "1.0.0", "manifest.json", '{"name":"My ARC"}'
    )


upload_response = asyncio.run(upload_example())
print(upload_response)
# --8<-- [end:upload]

# --8<-- [start:download]
async def download_example():
    return await client.Packages.DownloadGenericFileAsync(
        project_id, "arc-export", "1.0.0", "manifest.json"
    )


content = asyncio.run(download_example())
print(content)
# --8<-- [end:download]

mock_client.stop()
assert not responses, "Expected every sample request to run."
assert package.Version == "1.0.0"
assert json.loads(content) == {"name": "My ARC"}
