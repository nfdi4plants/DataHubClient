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
    "https://hub.example/api/v4/projects/42/repository/branches?search=main",
    200,
    [
      {
        "name": "main",
        "default": true,
        "protected": true,
        "merged": false,
        "commit": {
          "id": "abc123def456",
          "short_id": "abc123de",
          "title": "Add ARC metadata",
          "message": "Add ARC metadata",
          "author_name": "Carl Correns",
          "author_email": "carl@example.org",
          "created_at": "2026-05-01T00:00:00Z"
        }
      }
    ]
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects/42/repository/branches/main",
    200,
    {
      "name": "main",
      "default": true,
      "protected": true,
      "merged": false,
      "commit": {
        "id": "abc123def456",
        "short_id": "abc123de",
        "title": "Add ARC metadata",
        "message": "Add ARC metadata",
        "author_name": "Carl Correns",
        "author_email": "carl@example.org",
        "created_at": "2026-05-01T00:00:00Z"
      }
    }
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects/42/repository/commits?path=README.md&ref_name=main",
    200,
    [
      {
        "id": "abc123def456",
        "short_id": "abc123de",
        "title": "Add ARC metadata",
        "message": "Add ARC metadata",
        "author_name": "Carl Correns",
        "author_email": "carl@example.org",
        "created_at": "2026-05-01T00:00:00Z"
      }
    ]
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects/42/repository/commits/abc123def456",
    200,
    {
      "id": "abc123def456",
      "short_id": "abc123de",
      "title": "Add ARC metadata",
      "message": "Add ARC metadata",
      "author_name": "Carl Correns",
      "author_email": "carl@example.org",
      "created_at": "2026-05-01T00:00:00Z"
    }
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

# --8<-- [start:list-branches]
async def list_branches_example():
    branches = await client.Repository.ListBranchesAsync(42, search="main")
    for branch in branches:
        print(branch.Name, branch.Commit.ShortId)


asyncio.run(list_branches_example())
# --8<-- [end:list-branches]

# --8<-- [start:get-branch]
async def get_branch_example():
    branch = await client.Repository.GetBranchAsync(42, "main")
    print(f"{branch.Name} points to {branch.Commit.Id}")


asyncio.run(get_branch_example())
# --8<-- [end:get-branch]

# --8<-- [start:list-commits]
async def list_commits_example():
    commits = await client.Repository.ListCommitsAsync(42, ref_name="main", path="README.md")
    for commit in commits:
        print(commit.ShortId, commit.Title)


asyncio.run(list_commits_example())
# --8<-- [end:list-commits]

# --8<-- [start:get-commit]
async def get_commit_example():
    commit = await client.Repository.GetCommitAsync(42, "abc123def456")
    print(f"{commit.Title} by {commit.AuthorName}")


asyncio.run(get_commit_example())
# --8<-- [end:get-commit]

mock_client.stop()
assert not responses, "Expected every documented call to run."
