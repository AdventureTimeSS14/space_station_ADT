using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mining;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTBulletDeflectComponent : Component
{
    [DataField]
    public float RequiredPenetration = 1f;

    [DataField]
    public LocId Popup = "adt-bullet-deflected";

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg");
}
