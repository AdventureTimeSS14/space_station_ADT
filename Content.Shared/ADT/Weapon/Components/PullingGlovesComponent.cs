using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weapon.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PullingGlovesComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionWeaponPull";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public float MaxDistance = 4f;
}
