//

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AlertCarvingComponent : Component
{
    [DataField]
    public EntityUid? User;

    [DataField]
    public SoundSpecifier? AlertSound = new SoundPathSpecifier("/Audio/ADT/Heretic/curse.ogg");

    [DataField]
    public int TeleportDelay = 5000;
}
