using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Lavaland;

[Serializable, NetSerializable]
public enum ADTGemVisuals : byte
{
    Analysed,
    Broken,
}

[Serializable, NetSerializable]
public enum ADTJewelryVisuals : byte
{
    Gem,
}
