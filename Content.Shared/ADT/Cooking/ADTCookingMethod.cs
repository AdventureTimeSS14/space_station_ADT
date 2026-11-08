using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Cooking;

[Serializable, NetSerializable]
public enum ADTCookingMethod : byte
{
    Raw,
    Fried,
    Boiled,
    Baked,
}
