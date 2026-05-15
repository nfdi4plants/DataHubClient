namespace DataHubClient

open Fable.Core
open Thoth.Json.Core

/// <summary>
/// A single file in a DataHub repository, as returned by the
/// <see href="https://docs.gitlab.com/ee/api/repository_files.html">GitLab Repository Files API</see>.
/// <c>Content</c> is encoded according to <c>Encoding</c> (typically base64).
/// </summary>
/// <param name="fileName">The file's name without its directory path.</param>
/// <param name="filePath">The file's full path relative to the repository root.</param>
/// <param name="size">The file size in bytes.</param>
/// <param name="encoding">The encoding of <paramref name="content"/> (typically <c>base64</c>).</param>
/// <param name="content">The file content, encoded as described by <paramref name="encoding"/>.</param>
/// <param name="refName">The branch, tag, or commit SHA the file was read from.</param>
/// <param name="blobId">The SHA of the file blob.</param>
/// <param name="commitId">The SHA of the last commit that touched the file.</param>
[<AttachMembers>]
type RepoFile
    (
        fileName: string,
        filePath: string,
        size: int,
        encoding: string,
        content: string,
        refName: string,
        blobId: string,
        commitId: string
    ) =
    /// The file's name without its directory path.
    member val FileName = fileName with get, set
    /// The file's full path relative to the repository root.
    member val FilePath = filePath with get, set
    /// The file size in bytes.
    member val Size = size with get, set
    /// The encoding of <c>Content</c> (typically <c>base64</c>).
    member val Encoding = encoding with get, set
    /// The file content, encoded as described by <c>Encoding</c>.
    member val Content = content with get, set
    /// The branch, tag, or commit SHA the file was read from.
    member val Ref = refName with get, set
    /// The SHA of the file blob.
    member val BlobId = blobId with get, set
    /// The SHA of the last commit that touched the file.
    member val CommitId = commitId with get, set

    /// Decodes a <see cref="T:DataHubClient.RepoFile"/> from its GitLab JSON representation.
    static member Decoder : Decoder<RepoFile> =
        Decode.object (fun get ->
            RepoFile(
                get.Required.Field "file_name" Decode.string,
                get.Required.Field "file_path" Decode.string,
                get.Required.Field "size" Decode.int,
                get.Required.Field "encoding" Decode.string,
                get.Required.Field "content" Decode.string,
                get.Required.Field "ref" Decode.string,
                get.Required.Field "blob_id" Decode.string,
                get.Required.Field "commit_id" Decode.string))

    /// <summary>Encodes a <see cref="T:DataHubClient.RepoFile"/> to its GitLab JSON representation.</summary>
    /// <param name="file">The repository file to encode.</param>
    static member Encoder(file: RepoFile) : IEncodable =
        Encode.object [
            "file_name", Encode.string file.FileName
            "file_path", Encode.string file.FilePath
            "size", Encode.int file.Size
            "encoding", Encode.string file.Encoding
            "content", Encode.string file.Content
            "ref", Encode.string file.Ref
            "blob_id", Encode.string file.BlobId
            "commit_id", Encode.string file.CommitId ]
