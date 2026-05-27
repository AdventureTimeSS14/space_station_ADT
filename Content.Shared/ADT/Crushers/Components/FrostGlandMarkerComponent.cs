using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FrostGlandMarkerComponent : Component
{
    [ViewVariables]
    public TimeSpan ExpireTime;

    [DataField, AutoNetworkedField]
    public float DamageMultiplier = 0.9f;
}
