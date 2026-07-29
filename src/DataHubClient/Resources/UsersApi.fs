namespace DataHubClient

open Fable.Core
open DataHubClient.Json

/// <summary>
/// Client for the GitLab Users API used by DataHubs.
/// See the <see href="https://docs.gitlab.com/ee/api/users.html">GitLab Users API</see>.
/// </summary>
/// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
/// <param name="auth">The authentication header applied to every request.</param>
/// <param name="http">The transport used to send requests.</param>
[<AttachMembers>]
type UsersApi(baseUrl: string, auth: Authentication, http: IHttpClient) =

    /// <summary>
    /// Gets the user the configured credential authenticates as, from GitLab's
    /// <c>GET /user</c> endpoint.
    /// </summary>
    /// <remarks>
    /// This is the only endpoint that populates <see cref="P:DataHubClient.User.Email"/>;
    /// users embedded in issues, merge requests, notes, and commits never carry it.
    /// Useful for attributing an action to whoever supplied the token.
    /// </remarks>
    member _.GetCurrentAsync() =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" [ "user" ] []
            let! response = http.SendAsync req
            return ResourceHelpers.decode User.decoder response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Gets a user by numeric identifier.</summary>
    /// <param name="userId">The user's unique, instance-wide numeric identifier.</param>
    member _.GetAsync(userId: int) =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" [ "users"; string userId ] []
            let! response = http.SendAsync req
            return ResourceHelpers.decode User.decoder response
        }
        |> ResourceHelpers.toPublic
