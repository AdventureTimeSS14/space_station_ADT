using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Bubblegum.Abilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumSummonNarsiComponent : Component
{
    [DataField]
    public int Count = 3;

    [DataField]
    public EntProtoId MinionPrototype = "MobHivelordBrood";

    [DataField]
    public float SearchRange = 6f;

    [DataField]
    public float MinDistance = 2f;
}
