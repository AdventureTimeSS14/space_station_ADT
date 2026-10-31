using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Moon;

[RegisterComponent, NetworkedComponent]
public sealed partial class LunarParadeProjectileComponent : Component
{
    [DataField]
    public int Bounces;

    [DataField]
    public int MaxBounces = 40;

    [DataField]
    public HashSet<EntityUid> Hit = new();
}
