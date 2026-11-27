//

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UnfathomableCurioShieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = true;

    [DataField, AutoNetworkedField]
    public TimeSpan ActivateTime;

    [DataField]
    public TimeSpan ActivateDelay = TimeSpan.FromSeconds(20);

    [DataField]
    public SoundSpecifier BlockSound = new SoundPathSpecifier("/Audio/ADT/Heretic/repulse.ogg");

    [DataField]
    public SoundSpecifier RechargeSound = new SoundPathSpecifier("/Audio/ADT/Heretic/curse.ogg");
}
