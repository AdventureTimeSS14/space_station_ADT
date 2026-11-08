using Content.Shared.Damage;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTBloodlettingComponent : Component
{
    [ViewVariables]
    public int Stacks;

    [ViewVariables]
    public int Cap;

    [ViewVariables]
    public DamageSpecifier Damage = new();

    [ViewVariables]
    public TimeSpan DecayInterval;

    [ViewVariables]
    public TimeSpan NextDecay;
}
