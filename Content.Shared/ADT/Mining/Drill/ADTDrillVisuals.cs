using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Mining.Drill;

[NetSerializable, Serializable]
public enum ADTDrillVisuals : byte
{
    Active,

    Error,

    Supported,

    FillLevel,
}

[NetSerializable, Serializable]
public enum ADTDrillVisualLayers : byte
{
    Base,

    Active,

    Lights,

    Error,
}

[NetSerializable, Serializable]
public enum ADTDrillBraceVisuals : byte
{
    Connected,
}

[NetSerializable, Serializable]
public enum ADTDrillBraceVisualLayers : byte
{
    Base,
}