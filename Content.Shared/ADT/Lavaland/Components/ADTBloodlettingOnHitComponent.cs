using Content.Shared.Damage;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTBloodlettingOnHitComponent : Component
{
    [DataField]
    public int Stacks = 6;

    [DataField]
    public int Cap = 7;

    [DataField]
    public bool RequireWielded = true;

    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    [DataField]
    public TimeSpan DecayInterval = TimeSpan.FromSeconds(0.6);

    [DataField]
    public TimeSpan DelayPerHit = TimeSpan.FromSeconds(0.5);
}
