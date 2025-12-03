using Robust.Shared.Serialization;

namespace Content.Shared.CharecterFlavor;

[Serializable, NetSerializable]
public sealed class OpenURLEvent : EntityEventArgs
{
    public string URL { get; }
    public OpenURLEvent(string url)
    {
        URL = url;
    }
}
[Serializable, NetSerializable]
public sealed class OpenURLMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public enum CharecterFlavorUiKey : byte
{
    Key
}

