# --8<-- [start:setup]
import asyncio
from datahub_client import DataHubClient, Authentication

url = "https://git.nfdi4plants.org"
public_client = DataHubClient(url, Authentication.Anonymous())
# --8<-- [end:setup]

# Offline CI harness: replace httpx.AsyncClient before the first request.
import json
from urllib.parse import parse_qs, urlparse
from unittest.mock import patch
import httpx

requests = []


class OfflineClient:
    async def request(self, method, request_url, headers=None, content=None):
        requests.append((method, request_url, dict(headers or [])))
        project_id = len(requests)
        if project_id > 3:
            raise AssertionError("Unexpected extra pagination request.")
        projects = [{
            "id": project_id, "name": f"ARC {project_id}", "path": f"arc-{project_id}",
            "path_with_namespace": f"examples/arc-{project_id}", "visibility": "public",
            "web_url": f"{url}/examples/arc-{project_id}",
        }] if project_id < 3 else []
        return httpx.Response(200, text=json.dumps(projects))

    async def aclose(self):
        pass


mock_client = patch("httpx.AsyncClient", OfflineClient)
mock_client.start()

# --8<-- [start:list]


async def list_public_projects():
    return await public_client.Projects.ListAllAsync(visibility="public")


projects = asyncio.run(list_public_projects())
print(f"Found {len(projects)} public projects")
# --8<-- [end:list]

mock_client.stop()

assert [project.Id for project in projects] == [1, 2]
assert len(requests) == 3
for method, request_url, headers in requests:
    assert method == "GET"
    query = parse_qs(urlparse(request_url).query)
    for name, value in {
        "pagination": "keyset", "order_by": "id", "sort": "asc",
        "per_page": "100", "visibility": "public",
    }.items():
        assert query[name] == [value]
    assert not ({"PRIVATE-TOKEN", "Authorization", "JOB-TOKEN", ""} & headers.keys())
assert "id_after" not in parse_qs(urlparse(requests[0][1]).query)
assert parse_qs(urlparse(requests[1][1]).query)["id_after"] == ["1"]
assert parse_qs(urlparse(requests[2][1]).query)["id_after"] == ["2"]
