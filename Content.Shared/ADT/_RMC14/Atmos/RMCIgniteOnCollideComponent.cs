// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using Content.Shared.Damage;
using Content.Shared.Physics;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCIgniteOnCollideComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? MaxStacks;

    [DataField, AutoNetworkedField]
    public int Intensity = 15;

    [DataField, AutoNetworkedField]
    public int Duration = 55;

    [DataField, AutoNetworkedField]
    public bool InitDamaged;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? TileDamage;

    [DataField, AutoNetworkedField]
    public CollisionGroup Collision = CollisionGroup.HighImpassable | CollisionGroup.MidImpassable | CollisionGroup.LowImpassable | CollisionGroup.BulletImpassable | CollisionGroup.InteractImpassable;

    [DataField, AutoNetworkedField]
    public Color BurnColor = Color.Orange;
}
