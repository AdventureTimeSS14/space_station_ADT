using Content.Shared.DoAfter;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Mobs;

namespace Content.Server.ADT.MidroundCustomization;

/// <summary>
/// Allows humanoids to change their appearance mid-round.
/// </summary>
[DataDefinition]
public sealed partial class ChangeSlotOnStateEntry
{
    [DataField(required: true)]
    public MobState State { get; set; }

    [DataField(required: true)]
    public MarkingCategories Category { get; set; }

    [DataField(required: true)]
    public int Slot { get; set; }

    [DataField(required: true)]
    public string Marking { get; set; } = string.Empty;

    [DataField]
    public List<Color> Colors { get; set; } = new();
}

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
    [DataField]
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
    public SoundSpecifier ChangeMarkingSound = new SoundPathSpecifier("/Audio/ADT/slime-hair.ogg")
    {
        Params = AudioParams.Default.WithVolume(-1f),
    };

    [DataField]
    public bool PlaySoundForVoiceChange = true;

    [DataField]
    public EntProtoId Action = "ActionSlimeHair";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Changes slot on state.
    /// </summary>
    [DataField]
    public List<ChangeSlotOnStateEntry> ChangeSlotOnState { get; set; } = new();

    public Dictionary<(MarkingCategories Category, int Slot), (string Marking, List<Color> Colors)> OriginalMarkings = new();
}
