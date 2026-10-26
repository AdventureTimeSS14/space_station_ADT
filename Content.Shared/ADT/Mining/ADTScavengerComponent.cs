using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mining;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTScavengerComponent : Component
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    [DataField]
    public float Range = 0.6f;

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(2);

    [DataField]
    public bool StoreEaten;

    [DataField]
    public int Capacity = 10;

    [DataField]
    public DamageSpecifier? HealOnEat;

    [DataField]
    public string ContainerId = "adt_scavenger_belly";

    [ViewVariables]
    public TimeSpan NextEat;
}
