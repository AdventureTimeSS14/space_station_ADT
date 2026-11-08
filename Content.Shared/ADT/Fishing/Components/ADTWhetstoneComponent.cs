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
    public int Uses = 2;

    [DataField]
    public DamageSpecifier Increment = new();

    [DataField]
    public FixedPoint2 MaxDamage = 30;

    [DataField]
    public bool RequiresSharp = true;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/screwdriver.ogg");
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTSharpenedComponent : Component
{
}
