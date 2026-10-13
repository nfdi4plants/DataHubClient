namespace DataHubClient

open Fable.Core

/// <summary>
/// A DataHub credential, reduced to the single HTTP header it contributes to a
/// request, or anonymous access without an authentication header. Construct one
/// via the static factory methods rather than directly.
/// See the <see href="https://docs.gitlab.com/ee/api/rest/authentication.html">GitLab REST API authentication docs</see>.
/// </summary>
/// <param name="header">The HTTP header name carrying the credential, or an empty string for anonymous access.</param>
/// <param name="value">The HTTP header value carrying the credential, or an empty string for anonymous access.</param>
[<AttachMembers>]
type Authentication private (header: string, value: string) =
    /// <summary>The HTTP header name carrying the credential, or an empty string for anonymous access.</summary>
    member _.Header = header
    /// <summary>The HTTP header value carrying the credential, or an empty string for anonymous access.</summary>
    member _.Value = value

    /// <summary>Accesses public resources without sending an authentication header.</summary>
    /// <returns>An anonymous credential with empty <c>Header</c> and <c>Value</c> properties.</returns>
    static member Anonymous() =
        Authentication("", "")

    /// <summary>Authenticates with a Personal Access Token via the <c>PRIVATE-TOKEN</c> header.</summary>
    /// <param name="token">The personal access token.</param>
    static member PersonalAccessToken(token: string) =
        Authentication("PRIVATE-TOKEN", token)

    /// <summary>Authenticates with an OAuth 2.0 token via the <c>Authorization: Bearer</c> header.</summary>
    /// <param name="token">The OAuth 2.0 access token.</param>
    static member OAuthToken(token: string) =
        Authentication("Authorization", "Bearer " + token)

    /// <summary>Authenticates with a CI/CD job token via the <c>JOB-TOKEN</c> header.</summary>
    /// <param name="token">The CI/CD job token.</param>
    static member JobToken(token: string) =
        Authentication("JOB-TOKEN", token)
