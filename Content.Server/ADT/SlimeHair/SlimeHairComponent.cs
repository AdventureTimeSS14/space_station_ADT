using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Server.ADT.SlimeHair;

/// <summary>
/// Allows humanoids to change their appearance mid-round.
/// </summary>
[RegisterComponent]
public sealed partial class SlimeHairComponent : Component
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
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AddSlotTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// doafter time required to remove a existing slot
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RemoveSlotTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// doafter time required to change slot
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SelectSlotTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// doafter time required to recolor slot
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ChangeSlotTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Sound emitted when slots are changed
    /// </summary>
    [DataField]
    public SoundSpecifier ChangeHairSound = new SoundPathSpecifier("/Audio/ADT/slime-hair.ogg")
    {
        Params = AudioParams.Default.WithVolume(-1f),
    };

    [DataField("hairAction")]
    public EntProtoId Action = "ActionSlimeHair";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

}
