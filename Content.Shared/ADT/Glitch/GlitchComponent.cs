using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Glitch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GlitchComponent : Component
{
    [AutoNetworkedField]
    public TimeSpan ExpiresAt;
}
