namespace DataHubClient

open Fable.Core

/// <summary>
/// Version information for the DataHubClient library, shared by the .NET,
/// JavaScript/TypeScript, and Python packages.
/// </summary>
[<AttachMembers>]
type DataHubClientVersion =

    /// <summary>The package version compiled into this DataHubClient build.</summary>
    static member Value = "0.2.0"

    /// <summary>A user-agent compatible product token for this DataHubClient build.</summary>
    static member UserAgent = "DataHubClient/0.2.0"

    /// <summary>The request header used to report the DataHubClient version to a DataHub.</summary>
    static member HeaderName = "X-DataHubClient-Version"
