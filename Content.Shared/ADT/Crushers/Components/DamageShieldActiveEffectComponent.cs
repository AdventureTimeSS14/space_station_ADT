using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageShieldActiveEffectComponent : Component
{
    [ViewVariables]
    public TimeSpan ExpireTime;

    [DataField, AutoNetworkedField]
    public float DamageReductionMultiplier = 0.1f;
}
