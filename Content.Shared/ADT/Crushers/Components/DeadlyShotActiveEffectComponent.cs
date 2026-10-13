using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DeadlyShotActiveEffectComponent : Component
{
    [ViewVariables]
    public TimeSpan ExpireTime;
}
