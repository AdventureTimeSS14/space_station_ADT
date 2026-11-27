using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Moon;

[RegisterComponent, NetworkedComponent]
public sealed partial class MoonAscensionAuraComponent : Component
{
    [DataField]
    public float Radius = 7f;

    [DataField]
    public float SanityDamage = 20f;

    [DataField]
    public float ConvertThreshold = 10f;

    [DataField]
    public float TickInterval = 2f;

    [DataField]
    public float Accumulator;
}
