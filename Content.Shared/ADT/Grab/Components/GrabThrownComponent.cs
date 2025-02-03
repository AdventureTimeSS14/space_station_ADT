using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Grab;

[RegisterComponent, NetworkedComponent]
public sealed partial class GrabThrownComponent : Component
{
    public int MaxCollides = 2;
    public int CollideCounter = 0;

    public List<EntityUid> HitEntities = new();
}
