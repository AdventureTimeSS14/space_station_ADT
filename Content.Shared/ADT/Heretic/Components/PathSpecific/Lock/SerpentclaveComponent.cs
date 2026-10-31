//

using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components.PathSpecific.Lock;

[RegisterComponent, NetworkedComponent]
public sealed partial class SerpentclaveComponent : Component
{
    [DataField]
    public TimeSpan DoAfterTime = TimeSpan.FromSeconds(1.5);
}
