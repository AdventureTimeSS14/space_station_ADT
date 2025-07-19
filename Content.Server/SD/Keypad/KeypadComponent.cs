using Content.Shared.DeviceLinking;
using Content.Shared._SD.Keypad;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._SD.Keypad;

[RegisterComponent, Access(typeof(KeypadSystem))]
public sealed partial class KeypadComponent : Component
{
    [DataField("correctCode")]
    public string CorrectCode = "1234";

    [DataField("currentCode")]
    public string CurrentCode = "";

    [DataField("maxCodeLength")]
    public int MaxCodeLength = 4;

    [DataField("unlockedPort")]
    public string UnlockedPort = "Pressed";

    [DataField("keypadPressSound")]
    public SoundSpecifier KeypadPressSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

    [DataField("accessGrantedSound")]
    public SoundSpecifier AccessGrantedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/confirm_beep.ogg");

    [DataField("accessDeniedSound")]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

    /// <summary>
    /// The current operational state of the keypad.
    /// </summary>
    [DataField("state"), ViewVariables(VVAccess.ReadWrite)]
    public KeypadState State = KeypadState.Normal;
}
