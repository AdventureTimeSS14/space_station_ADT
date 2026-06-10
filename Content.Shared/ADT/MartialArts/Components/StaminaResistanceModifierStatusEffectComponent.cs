using Robust.Shared.GameStates;

namespace Content.Shared.ADT.MartialArts;

[RegisterComponent, NetworkedComponent]
public sealed partial class StaminaResistanceModifierStatusEffectComponent : Component
{
    [DataField]
    public float Modifier = 0.5f;
}
