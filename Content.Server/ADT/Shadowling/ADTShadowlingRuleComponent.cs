using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Shadowling;

[RegisterComponent, Access(typeof(ADTShadowlingRuleSystem))]
public sealed partial class ADTShadowlingRuleComponent : Component
{
    public readonly List<EntityUid> Minds = new();

    [DataField]
    public int ThrallDivisor = 4;

    [DataField]
    public int MinThralls = 8;

    [DataField]
    public int MaxThralls = 18;

    [ViewVariables]
    public int RequiredThralls;

    [DataField]
    public float WarningFraction = 0.66f;

    [ViewVariables]
    public bool WarningAnnounced;

    [ViewVariables]
    public bool Ascended;

    [DataField]
    public LocId WarningAnnouncement = "shadowling-warning-announcement";

    [DataField]
    public LocId WarningAnnouncer = "shadowling-warning-announcer";

    [DataField]
    public SoundSpecifier? BriefingSound = new SoundPathSpecifier("/Audio/ADT/hallucinations/veryfar_noise.ogg");

    [DataField]
    public EntProtoId MindRole = "MindRoleShadowling";

    [DataField]
    public EntProtoId AscendObjective = "ADTShadowlingAscendObjective";

    [DataField]
    public TimeSpan ForcedEvacuation = TimeSpan.FromMinutes(10);
}
