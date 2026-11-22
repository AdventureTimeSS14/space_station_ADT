using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weapons.Melee.ThrowOnHit;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTThrowOnHitRestrictionsComponent : Component
{
    [DataField]
    public bool MobsOnly = true;

    [DataField]
    public bool BlockFromContainer = true;

    [DataField]
    public bool BlockWeightless = true;
}
