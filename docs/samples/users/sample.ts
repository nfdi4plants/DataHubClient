// --8<-- [start:setup]
import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";

const client = new DataHubClient(
  "https://hub.example", Authentication.PersonalAccessToken("your-datahub-pat"));
// --8<-- [end:setup]

// Offline harness: validate requests through the shipped fetch transport.
const responses: Array<[method: string, url: string, status: number, body: unknown]> = [
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
];
const originalFetch = globalThis.fetch;
globalThis.fetch = async (url, init) => {
  const expected = responses.shift();
  if (!expected || init?.method !== expected[0] || url !== expected[1])
    throw new Error(`Unexpected request: ${init?.method} ${url}`);
  return new Response(expected[3] === null ? null : JSON.stringify(expected[3]), { status: expected[2] });
};

// --8<-- [start:get-current]
const currentUser = await client.Users.GetCurrentAsync();
console.log(currentUser.Username, currentUser.Email ?? "No email");
// --8<-- [end:get-current]

// --8<-- [start:get]
const user = await client.Users.GetAsync(7);
console.log(user.Username, user.WebUrl);
// --8<-- [end:get]

globalThis.fetch = originalFetch;
if (responses.length !== 0) throw new Error("Expected every documented call to run.");
