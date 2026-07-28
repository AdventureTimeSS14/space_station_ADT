using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KineticCooldownTrophyEffectComponent : Component
{
    [DataField]
    public float MeleeCoefficient = 1.3f;

    [DataField]
    public float RangedCoefficient = 1f;

    [DataField, AutoNetworkedField]
    public bool Applied;
}
