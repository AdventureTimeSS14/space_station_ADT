using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Combat;

[RegisterComponent, NetworkedComponent]
public sealed partial class NecksnapComponent : Component
{
    [DataField("sound")]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/ADT/Effects/crack1.ogg");

    [DataField("popup")]
    public LocId? Popup = "cqc-necksnap-popup";

}
