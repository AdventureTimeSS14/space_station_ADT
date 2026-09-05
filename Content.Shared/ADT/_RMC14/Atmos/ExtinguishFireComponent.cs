// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using Content.Shared.Physics;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent]
public sealed partial class ExtinguishFireComponent : Component
{
    [ViewVariables]
    public bool Extinguished;

    [DataField]
    public CollisionGroup Collision = CollisionGroup.MobLayer | CollisionGroup.MobMask;
}
