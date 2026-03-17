using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Traits;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FastLockersComponent : Component
{
    public const double CooldownTime = 1.5;

    [AutoNetworkedField]
    public TimeSpan CooldownEnd;
}
