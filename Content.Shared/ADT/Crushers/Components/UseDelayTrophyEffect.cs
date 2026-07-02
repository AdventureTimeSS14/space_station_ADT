using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UseDelayTrophyEffectComponent : Component
{
    [DataField]
    public float Coefficient = 1.3f;

    [DataField, AutoNetworkedField]
    public TimeSpan? OriginalDelay;
}
