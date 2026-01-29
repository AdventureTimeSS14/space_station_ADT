using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.TeddyBear;

[RegisterComponent, NetworkedComponent]
public sealed partial class TeddyBearComponent : Component
{
    [DataField]
    public EntProtoId WeaponPrototype = "WeaponLightMachineGunL6";

    [DataField]
    public TimeSpan? ExplodeTime;

    [DataField]
    public TimeSpan NextBeepTime;

    [DataField]
    public int BeepCount;
}

public sealed partial class TeddyBearSpawnGunEvent : InstantActionEvent { }
public sealed partial class TeddyBearExplodeEvent : InstantActionEvent { }
