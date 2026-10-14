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
    "https://hub.example/api/v4/user",
    200,
    {
      "id": 7,
      "username": "carl",
      "name": "Carl Correns",
      "state": "active",
      "web_url": "https://hub.example/carl",
      "email": "carl@example.org"
    }
  ],
  [
    "GET",
    "https://hub.example/api/v4/users/7",
    200,
    {
      "id": 7,
      "username": "carl",
      "name": "Carl Correns",
      "state": "active",
      "web_url": "https://hub.example/carl"
    }
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

# --8<-- [start:get-current]
async def get_current_example():
    current_user = await client.Users.GetCurrentAsync()
    print(current_user.Username, current_user.Email or "No email")


asyncio.run(get_current_example())
# --8<-- [end:get-current]

# --8<-- [start:get]
async def get_example():
    user = await client.Users.GetAsync(7)
    print(user.Username, user.WebUrl)


asyncio.run(get_example())
# --8<-- [end:get]

mock_client.stop()
assert not responses, "Expected every documented call to run."
