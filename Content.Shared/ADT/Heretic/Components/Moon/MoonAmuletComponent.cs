using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Moon;

[RegisterComponent, NetworkedComponent]
public sealed partial class MoonAmuletComponent : Component
{
    [DataField]
    public float SanityDamage = 20f;

    [DataField]
    public float ConvertThreshold = 25f;
}
