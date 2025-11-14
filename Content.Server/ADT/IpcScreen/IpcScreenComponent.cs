using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.IpcScreen;

[RegisterComponent]
public sealed partial class IpcScreenComponent : Component
{
    [DataField]
    public DoAfterId? DoAfter;

    /// <summary>
    /// Magic mirror target, used for validating UI messages.
    /// </summary>
    [DataField]
    public EntityUid? Target;

    /// <summary>
    /// doafter time required to add a new slot
    /// </summary>
    [DataField("addSlotTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AddSlotTime = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// doafter time required to remove a existing slot
    /// </summary>
    [DataField("removeSlotTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RemoveSlotTime = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// doafter time required to change slot
    /// </summary>
    [DataField("selectSlotTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SelectSlotTime = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// doafter time required to recolor slot
    /// </summary>
    [DataField("changeSlotTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ChangeSlotTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// doafter time required to change voice
    /// </summary>
    [DataField("changeVoiceTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ChangeVoiceTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Sound emitted when slots are changed
    /// </summary>
    [DataField("changeHairSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ChangeHairSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg")
    {
        Params = AudioParams.Default.WithVolume(-1f),
    };

    [DataField("hairAction"), AutoNetworkedField]
    public EntProtoId Action = "ActionIpcScreen";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

}
