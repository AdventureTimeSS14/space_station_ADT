using Content.Shared.ADT.TTS;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Dissociative.Components;

[RegisterComponent]
public sealed partial class DissociativeIdentityComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry AlterComponents = new();

    [DataField]
    public List<string> CopiedSlots = new() { "jumpsuit", "outerClothing", "shoes", "gloves", "head", "mask", "eyes", "neck" };

    [DataField]
    public HashSet<HumanoidVisualLayers> AlterLayers = new() { HumanoidVisualLayers.Hair, HumanoidVisualLayers.FacialHair };

    [DataField]
    public float LeashRange = 7f;

    [DataField]
    public TimeSpan FrontCooldown = TimeSpan.FromMinutes(7);

    [DataField]
    public TimeSpan FrontDuration = TimeSpan.FromSeconds(120);

    [DataField]
    public TimeSpan AbstinenceThreshold = TimeSpan.FromMinutes(20);

    [DataField]
    public TimeSpan SuppressionWindow = TimeSpan.FromSeconds(10);

    [DataField]
    public EntProtoId ManifestAction = "ActionDissociativeManifest";

    [DataField]
    public EntProtoId FrontAction = "ActionDissociativeFront";

    [DataField]
    public EntProtoId SetupAction = "ActionDissociativeSetup";

    [DataField]
    public EntProtoId ReleaseAction = "ActionDissociativeRelease";

    [DataField]
    public EntProtoId SurrenderAction = "ActionDissociativeSurrender";

    [DataField]
    public EntProtoId GiveControlAction = "ActionDissociativeGiveControl";

    public const string ContainerId = "dissociative-alter";

    [ViewVariables]
    public ContainerSlot Container = default!;

    [ViewVariables]
    public EntityUid? Alter;

    [ViewVariables]
    public bool Fronting;

    [ViewVariables]
    public EntityUid? HostMind;

    [ViewVariables]
    public EntityUid? AlterMind;

    [ViewVariables]
    public TimeSpan NextFront;

    [ViewVariables]
    public TimeSpan FrontEnd;

    [ViewVariables]
    public TimeSpan LastAntipsychotic;

    [ViewVariables]
    public EntityUid? ReleaseActionEntity;

    [ViewVariables]
    public EntityUid? SurrenderActionEntity;

    [ViewVariables]
    public EntityUid? GiveControlActionEntity;

    [ViewVariables]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>? StoredHostMarkings;

    [ViewVariables]
    public ProtoId<TTSVoicePrototype>? StoredHostVoice;
}
