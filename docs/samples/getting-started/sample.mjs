import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";

const url = "https://git.nfdi4plants.org";

// PersonalAccessToken, OAuthToken, and JobToken cover the GitLab credential types.
const auth = Authentication.PersonalAccessToken("your-datahub-pat");

// The client defaults to the HTTP transport built for the host runtime;
// assign `client.Http` to substitute your own (retries, proxies, tests).
const client = new DataHubClient(url, auth);

console.log(`DataHubClient ready against ${url}`);
