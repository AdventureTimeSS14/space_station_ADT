using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Grab;

[RegisterComponent, NetworkedComponent]
public sealed partial class GrabThrownComponent : Component
{
    public DamageSpecifier? DamageOnCollide;

    public List<EntityUid> IgnoreEntity = new();
}
