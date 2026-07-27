using Robust.Shared.Serialization;

namespace Content.Shared.ADT.GPS;

[Serializable, NetSerializable]
public enum GpsUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum GpsVisuals : byte
{
    Tracking,
    Emped,
}

[Serializable, NetSerializable]
public enum GpsVisualLayers : byte
{
    Working,
    Emp,
}

[Serializable, NetSerializable]
public sealed class GpsSignalData
{
    public string Tag;
    public string? Description;
    public Color Color;

    public Vector2i? Position;

    public bool SameMap;

    public GpsSignalData(string tag, string? description, Color color, Vector2i? position, bool sameMap)
    {
        Tag = tag;
        Description = description;
        Color = color;
        Position = position;
        SameMap = sameMap;
    }
}

[Serializable, NetSerializable]
public sealed class GpsBoundUserInterfaceState : BoundUserInterfaceState
{
    public bool Emped;

    public bool Tracking;
    public string Tag;
    public bool SameMapOnly;
    public bool CanToggle;

    public Vector2i? Position;

    public string? Location;

    public List<GpsSignalData> Signals;

    public GpsBoundUserInterfaceState(
        bool emped,
        bool tracking,
        string tag,
        bool sameMapOnly,
        bool canToggle,
        Vector2i? position,
        string? location,
        List<GpsSignalData> signals)
    {
        Emped = emped;
        Tracking = tracking;
        Tag = tag;
        SameMapOnly = sameMapOnly;
        CanToggle = canToggle;
        Position = position;
        Location = location;
        Signals = signals;
    }
}

[Serializable, NetSerializable]
public sealed class GpsToggleMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class GpsToggleRangeMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class GpsSetTagMessage : BoundUserInterfaceMessage
{
    public string Tag;

    public GpsSetTagMessage(string tag)
    {
        Tag = tag;
    }
}
