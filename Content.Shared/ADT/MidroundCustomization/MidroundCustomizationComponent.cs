using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server.ADT.MidroundCustomization;

[DataDefinition]
public sealed partial class ChangeSlotOnStateEntry
{
    [DataField(required: true)]
    public MobState State;

    [DataField]
    public ProtoId<OrganCategoryPrototype> Organ = "Head";

    [DataField(required: true)]
    public HumanoidVisualLayers Layer;

    [DataField]
    public int Slot;

    [DataField(required: true)]
    public string Marking = string.Empty;

    [DataField]
    public List<Color> Colors = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MidroundCustomizationComponent : Component
{
    [DataField]
    public DoAfterId? DoAfter;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField]
    public TimeSpan AddSlotTime = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan RemoveSlotTime = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan SelectSlotTime = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan ChangeSlotTime = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan ChangeVoiceTime = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier ChangeHairSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg")
    {
        Params = AudioParams.Default.WithVolume(-1f),
    };

    [DataField]
    public bool PlaySoundForVoiceChange = true;

    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ActionMidroundCustomization";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public HashSet<HumanoidVisualLayers> AllowedLayers = [];

    [DataField]
    public bool PointLightColor;

    [DataField]
    public HumanoidVisualLayers PointLightLayer = HumanoidVisualLayers.FacialHair;

    [DataField, AutoNetworkedField]
    public bool PointLightColorEnabled;

    [ViewVariables]
    public Color OriginalPointLightColor = Color.White;

    [DataField]
    public List<ChangeSlotOnStateEntry> ChangeSlotOnState = new();

    [ViewVariables]
    public Dictionary<(ProtoId<OrganCategoryPrototype> Organ, HumanoidVisualLayers Layer), List<Marking>> OriginalMarkings = new();
}
