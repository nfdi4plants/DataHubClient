# --8<-- [start:setup]
import asyncio
from pathlib import Path
from datahub_client import DataHubClient, Authentication

client = DataHubClient("https://git.nfdi4plants.org", Authentication.Anonymous())
project_id = 42
# --8<-- [end:setup]

# Offline harness: exercise HTTPX redirects without network access.
import os
from tempfile import TemporaryDirectory
from unittest.mock import patch
import httpx

fixture = '{"@context":"https://w3id.org/ro/crate/1.1/context","@graph":[{"@id":"./","@type":"Dataset","name":"Pflanzen – étude"}]}'
expected_url = "https://git.nfdi4plants.org/api/v4/projects/42/packages/generic/isa_arc_json/0.0.1/arc-ro-crate-metadata.json"
storage_url = "https://object-storage.example/exports/arc-ro-crate-metadata.json"
requests = []
real_async_client = httpx.AsyncClient


def offline_response(request):
    url = str(request.url)
    requests.append((request.method, url))
    assert request.method == "GET"
    assert not ({"private-token", "authorization", "job-token", ""} & request.headers.keys())
    if url == expected_url:
        return httpx.Response(302, headers={"Location": storage_url})
    assert url == storage_url, url
    return httpx.Response(200, text=fixture)


def offline_client(*args, **kwargs):
    return real_async_client(*args, transport=httpx.MockTransport(offline_response), **kwargs)


mock_client = patch("httpx.AsyncClient", offline_client)
mock_client.start()
original_directory = Path.cwd()
temporary_directory = TemporaryDirectory(prefix="datahubclient-ro-crate-")
os.chdir(temporary_directory.name)

# --8<-- [start:download]


async def download_ro_crate():
    metadata = await client.Packages.DownloadROCrateAsync(project_id)
    Path("arc-ro-crate-metadata.json").write_text(metadata, encoding="utf-8", newline="")
    print("Saved arc-ro-crate-metadata.json")


asyncio.run(download_ro_crate())
# --8<-- [end:download]

try:
    assert requests == [("GET", expected_url), ("GET", storage_url)]
    assert Path("arc-ro-crate-metadata.json").read_bytes() == fixture.encode("utf-8")
finally:
    os.chdir(original_directory)
    mock_client.stop()
    temporary_directory.cleanup()
