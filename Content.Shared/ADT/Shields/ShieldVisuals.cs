using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Shields;

[Serializable, NetSerializable]
public enum ShieldGeneratorVisuals
{
    Running,
    CapacitorNorth,
    CapacitorEast,
    CapacitorWest,
}

[Serializable, NetSerializable]
public enum ShieldGeneratorVisualLayers : byte
{
    Base,
    CapacitorNorth,
    CapacitorEast,
    CapacitorWest,
}

[Serializable, NetSerializable]
public enum ShieldSegmentVisuals
{
    Active,
    Overcharged,
    Floor,
}

[Serializable, NetSerializable]
public enum ShieldSegmentVisualLayers : byte
{
    Base,
}

[Serializable, NetSerializable]
public enum ShieldConduitVisuals
{
    Connected,
}

[Serializable, NetSerializable]
public enum ShieldConduitVisualLayers : byte
{
    Base,
}
[Serializable, NetSerializable]
public enum ShieldDiffuserVisuals
{
    State,
}

[Serializable, NetSerializable]
public enum ShieldDiffuserVisualLayers : byte
{
    Base,
}

[Serializable, NetSerializable]
public enum ShieldDiffuserState : byte
{
    On,
    Off,
    Emergency,
}

[Serializable, NetSerializable]
public enum HandheldShieldDiffuserVisuals
{
    Enabled,
}

[Serializable, NetSerializable]
public enum HandheldShieldDiffuserVisualLayers : byte
{
    Base,
}