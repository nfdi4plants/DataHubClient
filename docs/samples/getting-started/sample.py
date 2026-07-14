from datahub_client import DataHubClient, Authentication

url = "https://git.nfdi4plants.org"

# PersonalAccessToken, OAuthToken, and JobToken cover the GitLab credential types.
auth = Authentication.PersonalAccessToken("your-datahub-pat")

# The client defaults to the HTTP transport built for the host runtime;
# assign `client.Http` to substitute your own (retries, proxies, tests).
client = DataHubClient(url, auth)

print(f"DataHubClient ready against {url}")
