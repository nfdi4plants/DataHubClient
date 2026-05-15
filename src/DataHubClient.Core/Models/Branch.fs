namespace DataHubClient

open Fable.Core
open Thoth.Json.Core

/// <summary>
/// A branch in a DataHub repository, together with the commit it points at.
/// See the <see href="https://docs.gitlab.com/ee/api/branches.html">GitLab Branches API</see>.
/// </summary>
/// <param name="name">The branch name.</param>
/// <param name="isDefault">Whether this is the repository's default branch.</param>
/// <param name="isProtected">Whether the branch is protected against force-pushes and deletion.</param>
/// <param name="merged">Whether the branch has been merged into the default branch.</param>
/// <param name="commit">The commit the branch currently points at.</param>
[<AttachMembers>]
type Branch(name: string, isDefault: bool, isProtected: bool, merged: bool, commit: Commit) =
    /// The branch name.
    member val Name = name with get, set
    /// Whether this is the repository's default branch.
    member val Default = isDefault with get, set
    /// Whether the branch is protected against force-pushes and deletion.
    member val Protected = isProtected with get, set
    /// Whether the branch has been merged into the default branch.
    member val Merged = merged with get, set
    /// The commit the branch currently points at.
    member val Commit = commit with get, set

    /// Decodes a <see cref="T:DataHubClient.Branch"/> from its GitLab JSON representation.
    static member Decoder : Decoder<Branch> =
        Decode.object (fun get ->
            Branch(
                get.Required.Field "name" Decode.string,
                get.Required.Field "default" Decode.bool,
                get.Required.Field "protected" Decode.bool,
                get.Required.Field "merged" Decode.bool,
                get.Required.Field "commit" Commit.Decoder))

    /// <summary>Encodes a <see cref="T:DataHubClient.Branch"/> to its GitLab JSON representation.</summary>
    /// <param name="branch">The branch to encode.</param>
    static member Encoder(branch: Branch) : IEncodable =
        Encode.object [
            "name", Encode.string branch.Name
            "default", Encode.bool branch.Default
            "protected", Encode.bool branch.Protected
            "merged", Encode.bool branch.Merged
            "commit", Commit.Encoder branch.Commit ]
