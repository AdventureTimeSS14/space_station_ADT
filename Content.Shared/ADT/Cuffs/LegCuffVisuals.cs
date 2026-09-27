using Robust.Shared.Serialization;

namespace Content.Shared.Cuffs;

[Serializable, NetSerializable]
public enum LegCuffVisuals : byte
{
    Applied
}

[Serializable, NetSerializable]
public enum LegCuffVisualLayers : byte
{
    Overlay
}
