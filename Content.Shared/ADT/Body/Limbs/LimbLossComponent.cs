using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Body.Limbs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LimbLossComponent : Component
{
    [DataField, AutoNetworkedField]
    public LimbParts Missing;
}

[Flags, Serializable, NetSerializable]
public enum LimbParts : byte
{
    None = 0,
    ArmLeft = 1 << 0,
    ArmRight = 1 << 1,
    HandLeft = 1 << 2,
    HandRight = 1 << 3,
    LegLeft = 1 << 4,
    LegRight = 1 << 5,
    FootLeft = 1 << 6,
    FootRight = 1 << 7,

    Hands = ArmLeft | ArmRight | HandLeft | HandRight,
}
