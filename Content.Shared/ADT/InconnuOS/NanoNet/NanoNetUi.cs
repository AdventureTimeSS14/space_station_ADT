using Robust.Shared.Serialization;

namespace Content.Shared.ADT.InconnuOS.NanoNet;

[Serializable, NetSerializable]
public sealed class ADTOsNanoNetPublishMessage : BoundUserInterfaceMessage
{
    public readonly int RequestId;
    public readonly string Domain;
    public readonly string Html;

    public ADTOsNanoNetPublishMessage(int requestId, string domain, string html)
    {
        RequestId = requestId;
        Domain = domain;
        Html = html;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsNanoNetUnpublishMessage : BoundUserInterfaceMessage
{
    public readonly int RequestId;
    public readonly string Domain;

    public ADTOsNanoNetUnpublishMessage(int requestId, string domain)
    {
        RequestId = requestId;
        Domain = domain;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsNanoNetStatusMessage : BoundUserInterfaceMessage
{
    public readonly int RequestId;
    public readonly string Domain;
    public readonly bool Published;
    public readonly string OwnerName;
    public readonly TimeSpan PublishedAt;

    public ADTOsNanoNetStatusMessage(int requestId, string domain, bool published, string ownerName, TimeSpan publishedAt)
    {
        RequestId = requestId;
        Domain = domain;
        Published = published;
        OwnerName = ownerName;
        PublishedAt = publishedAt;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsNanoNetFetchRequestMessage : BoundUserInterfaceMessage
{
    public readonly int RequestId;
    public readonly string Domain;
    public readonly string Path;

    public ADTOsNanoNetFetchRequestMessage(int requestId, string domain, string path)
    {
        RequestId = requestId;
        Domain = domain;
        Path = path;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsNanoNetFetchResponseMessage : BoundUserInterfaceMessage
{
    public readonly int RequestId;
    public readonly string Domain;
    public readonly string Path;
    public readonly bool Found;
    public readonly string Html;
    public readonly string OwnerName;

    public ADTOsNanoNetFetchResponseMessage(int requestId, string domain, string path, bool found, string html, string ownerName)
    {
        RequestId = requestId;
        Domain = domain;
        Path = path;
        Found = found;
        Html = html;
        OwnerName = ownerName;
    }
}
