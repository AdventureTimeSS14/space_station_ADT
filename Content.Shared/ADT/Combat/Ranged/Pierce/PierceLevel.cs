using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Combat.Ranged.Pierce;

[Serializable, NetSerializable]
public enum PierceLevel : byte
{
    Flesh,
    Wood,
    Metal,
    HardenedMetal,
    Rock,
}
