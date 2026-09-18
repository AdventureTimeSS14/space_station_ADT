using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Fishing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTWhetstoneComponent : Component
{
    [DataField]
    public int Uses = 1;

    [DataField]
    public DamageSpecifier Increment = new();

    [DataField]
    public FixedPoint2 MaxDamage = 30;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Handling/generic_pickup.ogg");
}
