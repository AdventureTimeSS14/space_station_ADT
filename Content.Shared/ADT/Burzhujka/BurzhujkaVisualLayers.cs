using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Burzhujka;

[Serializable, NetSerializable]
public enum BurzhujkaVisualLayers : byte
{
    Main,
    Coals,
    Fire,
}
