using Robust.Shared.Prototypes;
using Content.Server.RoundEnd;
using Content.Shared.Roles;
using Content.Shared.Random;
using Robust.Shared.Audio;
using Content.Shared.Dataset;
using Content.Shared.Mind;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent]//, Access(typeof(PhantomRuleSystem))]
public sealed partial class PhantomRuleComponent : Component
{
    /// <summary>
    /// What will happen if all of the nuclear operatives will die. Used by LoneOpsSpawn event.
    /// </summary>
    [DataField]
    public RoundEndBehavior RoundEndBehavior = RoundEndBehavior.ShuttleCall;

    /// <summary>
    /// Text for shuttle call if RoundEndBehavior is ShuttleCall.
    /// </summary>
    [DataField]
    public string RoundEndTextSender = "comms-console-announcement-title-centcom";

    /// <summary>
    /// Text for shuttle call if RoundEndBehavior is ShuttleCall.
    /// </summary>
    [DataField]
    public string RoundEndTextShuttleCall = "phantom-no-more-threat-announcement-shuttle-call";

    /// <summary>
    /// Text for announcement if RoundEndBehavior is ShuttleCall. Used if shuttle is already called
    /// </summary>
    [DataField]
    public string RoundEndTextAnnouncement = "phantom-no-more-threat-announcement";

    /// <summary>
    /// Time to emergency shuttle to arrive if RoundEndBehavior is ShuttleCall.
    /// </summary>
    [DataField]
    public TimeSpan EvacShuttleTime = TimeSpan.FromMinutes(3);

    [DataField]
    public EntProtoId SpawnPointProto = "SpawnPointObserver";

    [DataField]
    public EntProtoId GhostSpawnPointProto = "SpawnPointGhostNukeOperative";

    [DataField]
    public string OperationName = "Test Operation";

    [DataField]
    public PhantomWinType WinType = PhantomWinType.Neutral;

    [DataField]
    public List<PhantomWinCondition> WinConditions = new();

    public EntityUid? TargetStation;

    /// <summary>
    ///     Data to be used in <see cref="OnMindAdded"/> for an operative once the Mind has been added.
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, string> OperativeMindPendingData = new();

    public Entity<MindComponent> PhantomMind = new();

    [DataField]
    public ProtoId<WeightedRandomPrototype> ObjectiveGroup = "PhantomObjectiveGroups";

    [DataField]
    public ProtoId<WeightedRandomPrototype> FinalObjectiveGroup = "PhantomFinalObjectiveGroup";

    //[DataField(required: true)]
    //public ProtoId<NpcFactionPrototype> Faction = default!;

    [DataField]
    public PhantomSpawnPreset PhantomSpawnDetails = new() { AntagRoleProto = "Phantom" };

    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/helping-hand-accept.ogg");

}

/// <summary>
/// Stores the presets for each operative type
/// Ie Commander, Agent and Operative
/// </summary>
[DataDefinition, Serializable]
public sealed partial class PhantomSpawnPreset
{

    [DataField]
    public ProtoId<AntagPrototype> AntagRoleProto = "Phantom";

}

public enum PhantomWinType : byte
{
    PhantomMajor,
    PhantomMinor,
    Neutral,
    CrewMinor,
    CrewMajor
}

public enum PhantomWinCondition : byte
{
    HeadsDead,
    NightmareStarted,
    PhantomReincarnated,
    PhantomAlive,
    TyranyAttemped,
    TyranySuccess,
    MaxPhantomLevelReached
}
