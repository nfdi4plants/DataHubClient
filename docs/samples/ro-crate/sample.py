# --8<-- [start:setup]
import asyncio
from pathlib import Path
from datahub_client import DataHubClient, Authentication

client = DataHubClient("https://git.nfdi4plants.org", Authentication.Anonymous())
project_id = 42
# --8<-- [end:setup]

# Offline harness: validate the real export coordinates without network access.
import os
from tempfile import TemporaryDirectory
from unittest.mock import patch
import httpx

fixture = '{"@context":"https://w3id.org/ro/crate/1.1/context","@graph":[{"@id":"./","@type":"Dataset","name":"Pflanzen – étude"}]}'
expected_url = "https://git.nfdi4plants.org/api/v4/projects/42/packages/generic/isa_arc_json/0.0.1/arc-ro-crate-metadata.json"
request_count = 0


class OfflineClient:
    async def request(self, method, url, headers=None, content=None):
        global request_count
        request_count += 1
        assert (method, url) == ("GET", expected_url)
        assert not ({"PRIVATE-TOKEN", "Authorization", "JOB-TOKEN", ""} & dict(headers or []).keys())
        return httpx.Response(200, text=fixture)

    async def aclose(self):
        pass


mock_client = patch("httpx.AsyncClient", OfflineClient)
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
    assert request_count == 1
    assert Path("arc-ro-crate-metadata.json").read_bytes() == fixture.encode("utf-8")
finally:
    os.chdir(original_directory)
    mock_client.stop()
    temporary_directory.cleanup()
