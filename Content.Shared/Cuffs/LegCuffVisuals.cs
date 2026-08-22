using Robust.Shared.Serialization;

namespace Content.Shared.Cuffs;

[Serializable, NetSerializable]
public enum LegCuffVisuals : byte
{
    Cuffed
}

[Serializable, NetSerializable]
public enum LegCuffVisualLayers : byte
{
    Base
}
