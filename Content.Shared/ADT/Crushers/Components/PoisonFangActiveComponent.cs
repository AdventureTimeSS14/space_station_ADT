using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PoisonFangActiveComponent : Component
{
    [ViewVariables]
    public TimeSpan ExpireTime;

    [DataField, AutoNetworkedField]
    public float DamageMult = 1.1f;
}

