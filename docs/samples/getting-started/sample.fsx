#r "nuget: DataHubClient"

open DataHubClient

let url = "https://git.nfdi4plants.org"

// PersonalAccessToken, OAuthToken, and JobToken cover the GitLab credential types.
let auth = Authentication.PersonalAccessToken "your-datahub-pat"

// The client defaults to the HTTP transport built for the host runtime;
// assign `client.Http` to substitute your own (retries, proxies, tests).
let client = DataHubClient(url, auth)

printfn $"DataHubClient ready against {url}"
