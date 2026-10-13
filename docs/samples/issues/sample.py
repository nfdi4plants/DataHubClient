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
        "[{\"id\":100,\"iid\":3,\"project_id\":42,\"title\":\"Add assay metadata\",\"state\":\"opened\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"assignees\":[],\"labels\":[],\"web_url\":\"https://git.nfdi4plants.org/examples/arc/-/issues/3\",\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\",\"description\":\"Describe the assay.\"}]"
    ],
    [
        "GET",
        "{\"id\":100,\"iid\":3,\"project_id\":42,\"title\":\"Add assay metadata\",\"state\":\"opened\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"assignees\":[],\"labels\":[],\"web_url\":\"https://git.nfdi4plants.org/examples/arc/-/issues/3\",\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\",\"description\":\"Describe the assay.\"}"
    ],
    [
        "POST",
        "{\"id\":100,\"iid\":3,\"project_id\":42,\"title\":\"Add assay metadata\",\"state\":\"opened\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"assignees\":[],\"labels\":[],\"web_url\":\"https://git.nfdi4plants.org/examples/arc/-/issues/3\",\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\",\"description\":\"Describe the assay.\"}"
    ],
    [
        "PUT",
        "{\"id\":100,\"iid\":3,\"project_id\":42,\"title\":\"Complete assay metadata\",\"state\":\"opened\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"assignees\":[],\"labels\":[],\"web_url\":\"https://git.nfdi4plants.org/examples/arc/-/issues/3\",\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\",\"description\":\"Add protocol details.\"}"
    ],
    [
        "PUT",
        "{\"id\":100,\"iid\":3,\"project_id\":42,\"title\":\"Add assay metadata\",\"state\":\"closed\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"assignees\":[],\"labels\":[],\"web_url\":\"https://git.nfdi4plants.org/examples/arc/-/issues/3\",\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\",\"description\":\"Describe the assay.\"}"
    ],
    [
        "GET",
        "[{\"id\":5,\"body\":\"Metadata reviewed.\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"system\":false,\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\"}]"
    ],
    [
        "POST",
        "{\"id\":5,\"body\":\"Metadata reviewed.\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"system\":false,\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\"}"
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

# --8<-- [start:list]
async def list_example():
    return await client.Issues.ListAsync(project_id, state="opened")


issues = asyncio.run(list_example())
for item in issues:
    print(f"{item.Iid}: {item.Title}")
# --8<-- [end:list]

# --8<-- [start:get]
async def get_example():
    return await client.Issues.GetAsync(project_id, 3)


issue = asyncio.run(get_example())
print(issue.Title)
# --8<-- [end:get]

# --8<-- [start:create]
async def create_example():
    return await client.Issues.CreateAsync(
        project_id, "Add assay metadata", description="Describe the assay."
    )


created = asyncio.run(create_example())
print(created.WebUrl)
# --8<-- [end:create]

# --8<-- [start:update]
async def update_example():
    return await client.Issues.UpdateAsync(
        project_id, 3, title="Complete assay metadata", description="Add protocol details."
    )


updated = asyncio.run(update_example())
print(updated.Title)
# --8<-- [end:update]

# --8<-- [start:close]
async def close_example():
    return await client.Issues.CloseAsync(project_id, 3)


closed = asyncio.run(close_example())
print(closed.State)
# --8<-- [end:close]

# --8<-- [start:notes]
async def notes_example():
    return await client.Issues.NotesAsync(project_id, 3)


notes = asyncio.run(notes_example())
for note in notes:
    print(note.Body)
# --8<-- [end:notes]

# --8<-- [start:create-note]
async def create_note_example():
    return await client.Issues.CreateNoteAsync(project_id, 3, "Metadata reviewed.")


note = asyncio.run(create_note_example())
print(note.Body)
# --8<-- [end:create-note]

mock_client.stop()
assert not responses, "Expected every sample request to run."
assert closed.State == "closed"
assert updated.Title == "Complete assay metadata"
assert note.Body == "Metadata reviewed."
