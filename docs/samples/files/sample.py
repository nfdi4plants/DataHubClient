# --8<-- [start:setup]
import asyncio
import base64
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
    "https://hub.example/api/v4/projects/42/repository/files/README.md?ref=main",
    200,
    {
      "file_name": "README.md",
      "file_path": "README.md",
      "size": 3,
      "encoding": "base64",
      "content": "QVJD",
      "ref": "main",
      "blob_id": "blob1",
      "commit_id": "commit1"
    }
  ],
  [
    "POST",
    "https://hub.example/api/v4/projects/42/repository/files/notes.txt",
    201,
    {
      "file_path": "notes.txt",
      "branch": "main"
    }
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects/42/repository/files/notes.txt?ref=main",
    200,
    {
      "file_name": "notes.txt",
      "file_path": "notes.txt",
      "size": 3,
      "encoding": "base64",
      "content": "QVJD",
      "ref": "main",
      "blob_id": "blob1",
      "commit_id": "commit1"
    }
  ],
  [
    "PUT",
    "https://hub.example/api/v4/projects/42/repository/files/notes.txt",
    200,
    {
      "file_path": "notes.txt",
      "branch": "main"
    }
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects/42/repository/files/notes.txt?ref=main",
    200,
    {
      "file_name": "notes.txt",
      "file_path": "notes.txt",
      "size": 3,
      "encoding": "base64",
      "content": "QVJD",
      "ref": "main",
      "blob_id": "blob1",
      "commit_id": "commit1"
    }
  ],
  [
    "DELETE",
    "https://hub.example/api/v4/projects/42/repository/files/notes.txt",
    204,
    null
  ]
]''')


class OfflineClient:
    def __init__(self, *, follow_redirects, **kwargs):
        assert follow_redirects is True

    async def request(self, method, url, headers=None, content=None):
        expected_method, expected_url, status, body = responses.pop(0)
        assert (method, url) == (expected_method, expected_url), (method, url)
        return httpx.Response(status, text="" if body is None else json.dumps(body))

    async def aclose(self):
        pass


mock_client = patch("httpx.AsyncClient", OfflineClient)
mock_client.start()

# --8<-- [start:get]
async def get_example():
    file = await client.Files.GetAsync(42, "README.md", "main")
    text = (base64.b64decode(file.Content).decode("utf-8")
            if file.Encoding == "base64" else file.Content)
    print(text)


asyncio.run(get_example())
# --8<-- [end:get]

# --8<-- [start:create]
async def create_example():
    created = await client.Files.CreateAsync(42, "notes.txt", "main", "ARC", "Add notes")
    print(f"Created {created.FilePath} on {created.Ref}")


asyncio.run(create_example())
# --8<-- [end:create]

# --8<-- [start:update]
async def update_example():
    updated = await client.Files.UpdateAsync(42, "notes.txt", "main", "QVJD", "Update notes", encoding="base64")
    print(f"Updated {updated.FilePath} at {updated.CommitId}")


asyncio.run(update_example())
# --8<-- [end:update]

# --8<-- [start:delete]
async def delete_example():
    await client.Files.DeleteAsync(42, "notes.txt", "main", "Remove notes")
    print("Deleted notes.txt")


asyncio.run(delete_example())
# --8<-- [end:delete]

mock_client.stop()
assert not responses, "Expected every documented call to run."
