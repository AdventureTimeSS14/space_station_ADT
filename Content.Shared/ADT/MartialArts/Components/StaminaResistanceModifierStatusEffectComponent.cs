using Robust.Shared.GameStates;

namespace Content.Shared.ADT.MartialArts;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StaminaResistanceModifierStatusEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Modifier = 1f;
}
