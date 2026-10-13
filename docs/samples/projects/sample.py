# --8<-- [start:setup]
import asyncio
from datahub_client import DataHubClient, Authentication

client = DataHubClient(
    "https://hub.example", Authentication.PersonalAccessToken("your-datahub-pat"))
# --8<-- [end:setup]

# Offline harness: validate requests through the shipped httpx transport.
import json
from unittest.mock import patch
import httpx

responses = json.loads(r'''[
  [
    "GET",
    "https://hub.example/api/v4/projects?search=plant%20arc&simple=true",
    200,
    [
      {
        "id": 42,
        "name": "My ARC",
        "path": "my-arc",
        "path_with_namespace": "lab/my-arc",
        "visibility": "public",
        "web_url": "https://hub.example/lab/my-arc",
        "description": "An annotated research context",
        "default_branch": "main"
      }
    ]
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects?order_by=id&pagination=keyset&per_page=100&sort=asc&visibility=public",
    200,
    [
      {
        "id": 42,
        "name": "My ARC",
        "path": "my-arc",
        "path_with_namespace": "lab/my-arc",
        "visibility": "public",
        "web_url": "https://hub.example/lab/my-arc",
        "description": "An annotated research context",
        "default_branch": "main"
      }
    ]
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects?id_after=42&order_by=id&pagination=keyset&per_page=100&sort=asc&visibility=public",
    200,
    []
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects/42",
    200,
    {
      "id": 42,
      "name": "My ARC",
      "path": "my-arc",
      "path_with_namespace": "lab/my-arc",
      "visibility": "public",
      "web_url": "https://hub.example/lab/my-arc",
      "description": "An annotated research context",
      "default_branch": "main"
    }
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects?search=plant%20arc",
    200,
    [
      {
        "id": 42,
        "name": "My ARC",
        "path": "my-arc",
        "path_with_namespace": "lab/my-arc",
        "visibility": "public",
        "web_url": "https://hub.example/lab/my-arc",
        "description": "An annotated research context",
        "default_branch": "main"
      }
    ]
  ]
]''')


class OfflineClient:
    async def request(self, method, url, headers=None, content=None):
        expected_method, expected_url, status, body = responses.pop(0)
        assert (method, url) == (expected_method, expected_url), (method, url)
        return httpx.Response(status, text="" if body is None else json.dumps(body))

    async def aclose(self):
        pass


mock_client = patch("httpx.AsyncClient", OfflineClient)
mock_client.start()

# --8<-- [start:list]
async def list_example():
    projects = await client.Projects.ListAsync(search="plant arc", simple=True)
    for project in projects:
        print(project.Id, project.PathWithNamespace)


asyncio.run(list_example())
# --8<-- [end:list]

# --8<-- [start:list-all]
async def list_all_example():
    all_projects = await client.Projects.ListAllAsync(visibility="public")
    print(f"Found {len(all_projects)} public projects")


asyncio.run(list_all_example())
# --8<-- [end:list-all]

# --8<-- [start:get]
async def get_example():
    project = await client.Projects.GetAsync(42)
    print(project.Name, project.WebUrl)


asyncio.run(get_example())
# --8<-- [end:get]

# --8<-- [start:search]
async def search_example():
    matches = await client.Projects.SearchAsync("plant arc")
    for project in matches:
        print(project.PathWithNamespace)


asyncio.run(search_example())
# --8<-- [end:search]

mock_client.stop()
assert not responses, "Expected every documented call to run."
