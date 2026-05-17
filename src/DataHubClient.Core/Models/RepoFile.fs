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
    let mutable _fileName = fileName
    let mutable _filePath = filePath
    let mutable _size = size
    let mutable _encoding = encoding
    let mutable _content = content
    let mutable _refName = refName
    let mutable _blobId = blobId
    let mutable _commitId = commitId

    /// The file's name without its directory path.
    member _.FileName with get () = _fileName and set value = _fileName <- value
    /// The file's full path relative to the repository root.
    member _.FilePath with get () = _filePath and set value = _filePath <- value
    /// The file size in bytes.
    member _.Size with get () = _size and set value = _size <- value
    /// The encoding of <c>Content</c> (typically <c>base64</c>).
    member _.Encoding with get () = _encoding and set value = _encoding <- value
    /// The file content, encoded as described by <c>Encoding</c>.
    member _.Content with get () = _content and set value = _content <- value
    /// The branch, tag, or commit SHA the file was read from.
    member _.Ref with get () = _refName and set value = _refName <- value
    /// The SHA of the file blob.
    member _.BlobId with get () = _blobId and set value = _blobId <- value
    /// The SHA of the last commit that touched the file.
    member _.CommitId with get () = _commitId and set value = _commitId <- value

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
