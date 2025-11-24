using Content.Shared.DoAfter;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.SlimeHair;

/// <summary>
/// Allows humanoids to change their appearance mid-round.
/// </summary>
[RegisterComponent]
public sealed partial class MidroundCustomizationComponent : Component
{
    #region DoAfter Settings
    [DataField]
    public DoAfterId? DoAfter;

    /// <summary>
    /// doafter time required to add a new slot
    /// </summary>
    [DataField]
    public TimeSpan AddSlotTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// doafter time required to remove a existing slot
    /// </summary>
    [DataField]
    public TimeSpan RemoveSlotTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// doafter time required to change slot
    /// </summary>
    [DataField,]
    public TimeSpan SelectSlotTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// doafter time required to recolor slot
    /// </summary>
    [DataField]
    public TimeSpan ChangeSlotTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// doafter time required to change voice
    /// </summary>
    [DataField]
    public TimeSpan ChangeVoiceTime = TimeSpan.FromSeconds(2);
    #endregion

    [DataField]
    public List<MarkingCategories> CustomizableCategories = new()
    {
        MarkingCategories.Hair,
        MarkingCategories.FacialHair,
    };

    [DataField]
    public bool DefaultSkinColoring = true;

    /// <summary>
    /// Sound emitted when slots are changed
    /// </summary>
    [DataField]
    public SoundSpecifier ChangeHairSound = new SoundPathSpecifier("/Audio/ADT/slime-hair.ogg")
    {
        Params = AudioParams.Default.WithVolume(-1f),
    };

    [DataField]
    public EntProtoId Action = "ActionSlimeHair";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

}
