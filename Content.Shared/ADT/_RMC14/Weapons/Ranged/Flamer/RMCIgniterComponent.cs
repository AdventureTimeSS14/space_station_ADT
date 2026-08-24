// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCIgniterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public bool Locked;

    [DataField, AutoNetworkedField]
    public SoundPathSpecifier? Sound = new("/Audio/Items/Lighters/lighter1.ogg");

    [DataField, AutoNetworkedField]
    public LocId Popup = "rmc-flamer-ignite-first";

    [DataField, AutoNetworkedField]
    public LocId PopupKey = "rmc-flamer-ignite-first-with";

    [DataField, AutoNetworkedField]
    public LocId ExamineText = "rmc-flamer-ignite-action-examine";
}
